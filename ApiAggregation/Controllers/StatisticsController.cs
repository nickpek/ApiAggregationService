using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class StatisticsController : ControllerBase
    {
        private readonly ApiStatisticsService _statisticsService;

        public StatisticsController(ApiStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet("request-stats")]
        public IActionResult GetRequestStatistics()
        {
            var stats = _statisticsService.GetStatistics();
            return Ok(stats);
        }
    }

}
