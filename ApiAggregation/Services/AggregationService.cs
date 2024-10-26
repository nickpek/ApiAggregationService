using ApiAggregation.Clients;
using Serilog;

namespace ApiAggregation.Services
{
    public class AggregationService
    {
        private readonly OpenWeatherClient _openWeatherClient;

        public AggregationService(OpenWeatherClient openWeatherClient)
        {
            _openWeatherClient = openWeatherClient;
        }
        public async Task<Dictionary<string, object>> GetAggregatedDataAsync(string? city = null)
        {
            var result = new Dictionary<string, object>();

            // Fetch weather data with error handling OpenWeatherClient
            try
            {
                var weatherData = await _openWeatherClient.GetDataAsync(city ?? "DefaultCity");
                result.Add("weather", weatherData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve weather data for city {City}", city);
            }

            return result;

        }
    }
}

