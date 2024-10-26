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
        public async Task<IActionResult> GetAggregatedData()
        {
            var data = await _aggregationService.GetAggregatedDataAsync();
            return Ok(data);
        }
    }
}
