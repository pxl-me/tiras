using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oscilloscope.Server.Application;

namespace Oscilloscope.Server.Api.Controllers
{
    [ApiController]
    [Route("api/anomalies")]
    [Authorize]
    public sealed class AnomaliesController : ControllerBase
    {
        private readonly IAnomalyQueryService _query;

        public AnomaliesController(IAnomalyQueryService query) => _query = query;

        [HttpGet("/latest")]
        public async Task<IActionResult> Latest([FromQuery] int take = 20, CancellationToken ct = default)
        {
            take = Math.Clamp(take, 1, 200);
            var items = await _query.GetLatestAsync(take, ct);
            return Ok(items);
        }
    }
}
