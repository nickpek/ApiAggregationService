using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly AggregationService _aggregationService;

        public DataController(AggregationService aggregationService)
        {
            _aggregationService = aggregationService;
        }

        [HttpGet("aggregated")]
        public async Task<IActionResult> GetAggregatedData(
               [FromQuery] string? city = null,
               [FromQuery] string? country = null,
               [FromQuery] bool sortByTeamName = false)
        {
            var data = await _aggregationService.GetAggregatedDataAsync(city, country, sortByTeamName);
            return Ok(data);
        }
    }
}
