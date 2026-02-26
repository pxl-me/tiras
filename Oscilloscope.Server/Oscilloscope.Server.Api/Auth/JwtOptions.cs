namespace Oscilloscope.Server.Api.Auth
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = "oscilloscope-api";
        public string Audience { get; set; } = "oscilloscope-client";
        public string Secret { get; set; } = literal:"";
        public int ExpMinutes { get; set; } = 120;
    }
}
