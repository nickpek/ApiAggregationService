using ApiAggregation.Clients;

namespace ApiAggregation.Services
{
    public class AggregationService
    {
        private readonly OpenWeatherClient _openWeatherClient;

        public AggregationService(OpenWeatherClient openWeatherClient)
        {
            _openWeatherClient = openWeatherClient;
        }
        public async Task<Dictionary<string, object>> GetAggregatedDataAsync()
        {
            var result = new Dictionary<string, object>();

            var weatherData = await _openWeatherClient.GetDataAsync();
            result.Add("weather", weatherData);

            return result;

        }
    }
}

