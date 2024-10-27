using System.Collections.Concurrent;

namespace ApiAggregation.Services
{
    public class ApiStatisticsService
    {
        public class ApiStats
        {
            public int TotalRequests { get; set; }
            public List<double> ResponseTimes { get; } = new List<double>();
        }

        private readonly ConcurrentDictionary<string, ApiStats> _apiStats = new();

        public void TrackRequest(string apiName, double responseTime)
        {
            var stats = _apiStats.GetOrAdd(apiName, _ => new ApiStats());
            stats.TotalRequests++;
            stats.ResponseTimes.Add(responseTime);
        }

        public Dictionary<string, object> GetStatistics()
        {
            var result = new Dictionary<string, object>();

            foreach (var (apiName, stats) in _apiStats)
            {
                var averageTime = stats.ResponseTimes.Count > 0 ? stats.ResponseTimes.Average() : 0;
                result[apiName] = new
                {
                    TotalRequests = stats.TotalRequests,
                    AverageResponseTime = averageTime,
                    FastRequests = stats.ResponseTimes.Count(t => t < 100),
                    AverageRequests = stats.ResponseTimes.Count(t => t >= 100 && t < 200),
                    SlowRequests = stats.ResponseTimes.Count(t => t >= 200)
                };
            }

            return result;
        }
    }
}
