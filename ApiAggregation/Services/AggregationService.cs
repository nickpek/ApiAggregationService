using ApiAggregation.Clients;
using Serilog;

namespace ApiAggregation.Services
{
    public class AggregationService
    {
        private readonly OpenWeatherClient _openWeatherClient;
        private readonly NewsApiClient _newsApiClient;


        public AggregationService(OpenWeatherClient openWeatherClient, NewsApiClient newsApiClient)
        {
            _openWeatherClient = openWeatherClient;
            _newsApiClient = newsApiClient;

        }
        public async Task<Dictionary<string, object>> GetAggregatedDataAsync(string? city = null, string? country = null)
        {
            var result = new Dictionary<string, object>();

            // Fetch weather data with error handling OpenWeatherClient
            try
            {
                var weatherData = await _openWeatherClient.GetDataAsync(city);
                result.Add("weather", weatherData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve weather data for city {City}", city);
            }
            // Fetch news data with error handling NewsApiClient
            try
            {
                var rawNewsData = await _newsApiClient.GetDataAsync(country);
                result.Add("news", rawNewsData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve news data for country {Country}", country);
            }
            return result;

        }
    }
}

