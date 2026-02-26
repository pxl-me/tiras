using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Oscilloscope.Server.Api.Auth;
using Oscilloscope.Server.Infrastructure;

namespace Oscilloscope.Server.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IJwtTokenService _jwt;

        public AuthController(UserManager<ApplicationUser> users, IJwtTokenService jwt)
        {
            _users = users;
            _jwt = jwt;
        }

        public sealed record RegisterRequest(string Email, string Password);
        public sealed record LoginRequest(string Email, string Password);

        [HttpPost("/register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
        {
            var user = new ApplicationUser { UserName = req.Email, Email = req.Email };
            var res = await _users.CreateAsync(user, req.Password);

            if (!res.Succeeded)
                return BadRequest(res.Errors.Select(e => e.Description));

            return Ok(_jwt.CreateToken(user));
        }

        [HttpPost("/login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
        {
            var user = await _users.FindByEmailAsync(req.Email);
            if (user is null) return Unauthorized();

            if (!await _users.CheckPasswordAsync(user, req.Password))
                return Unauthorized();

            return Ok(_jwt.CreateToken(user));
        }
    }
}
