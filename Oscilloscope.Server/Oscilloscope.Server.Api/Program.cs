using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Oscilloscope.Server.Api.Auth;
using Oscilloscope.Server.Api.Hubs;
using Oscilloscope.Server.Application;
using Oscilloscope.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<UdpOptions>(builder.Configuration.GetSection("Udp"));
builder.Services.Configure<SignalModelOptions>(builder.Configuration.GetSection("SignalModel"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// Postgres DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DB")));

// Identity (tofix)
builder.Services.AddIdentityCore<ApplicationUser>(opt =>
{
    opt.Password.RequiredLength = 6;
    opt.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>();

// JWT
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = key
        };

        opt.Events = new JwtBearerEvents
        {
            //WebSocket передає токен як access_token query
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/signal"))
                    ctx.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

// CORS
//якщо клієнт (web) на іншому origin, без CORS браузер заблокує запити/SignalR handshake.
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ClientCors", p =>
        p.WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Services
builder.Services.AddSingleton<ISignalAnalyzer, SignalAnalyzer>();
builder.Services.AddSingleton<IUdpPacketDeserializer, UdpPacketDeserializer>();
builder.Services.AddSingleton<ISignalStreamBroadcaster, InMemorySignalStreamBroadcaster>();

builder.Services.AddScoped<IAnomalyWriter, EfCoreAnomalyWriter>();
builder.Services.AddScoped<IAnomalyQueryService, EfCoreAnomalyQueryService>();
builder.Services.AddScoped<SignalProcessingService>();

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// HostedService
builder.Services.AddHostedService<UdpIngestHostedService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors("ClientCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SignalHub>("/hubs/signal");

app.Run();