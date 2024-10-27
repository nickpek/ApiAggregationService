using ApiAggregation.Clients;
using ApiAggregation.Utilities;
using Serilog;
using System.Globalization;

namespace ApiAggregation.Services
{
    public class AggregationService
    {
        private readonly IOpenWeatherClient _openWeatherClient;
        private readonly INewsApiClient _newsApiClient;
        private readonly IApiFootballClient _apiFootballClient;
        public AggregationService(IOpenWeatherClient openWeatherClient, INewsApiClient newsApiClient, IApiFootballClient apiFootballClient)
        {
            _openWeatherClient = openWeatherClient;
            _newsApiClient = newsApiClient;
            _apiFootballClient = apiFootballClient;
        }
        public async Task<Dictionary<string, object>> GetAggregatedDataAsync(string? city = null, string? country = null, bool sortByTeamName = true)
        {
            var result = new Dictionary<string, object>();
            var (countryCode, countryName) = ParseCountryInput(country);

            // Set up tasks for each API call to run them in parallel
            var weatherTask = GetWeatherDataAsync(city);
            var newsTask = GetNewsDataAsync(countryCode);
            var footballTask = GetFootballDataAsync(countryName, sortByTeamName);
            
            // Await all tasks to complete
            await Task.WhenAll(weatherTask, newsTask, footballTask ?? Task.CompletedTask);

            // Collect results from each completed task
            result["weather"] = weatherTask.Result ?? FallbackUtilites.GetWeatherFallback(city);
            result["news"] = newsTask.Result ?? FallbackUtilites.GetNewsFallback("News data currently unavailable.");
            result["football"] = footballTask?.Result ?? FallbackUtilites.GetTeamsFallback("Teams data currently unavailable.");

            return result;
        }

        private async Task<object?> GetWeatherDataAsync(string? city)
        {
            try
            {
                return await _openWeatherClient.GetDataAsync(city);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve weather data for city {City}", city);
                return null;
            }
        }

        private async Task<object?> GetNewsDataAsync(string? countryCode)
        {
            try
            {
                return await _newsApiClient.GetDataAsync(countryCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve news data for country {Country}", countryCode);
                return null;
            }
        }

        private async Task<object?> GetFootballDataAsync(string? countryName, bool sortByTeamName)
        {
            try
            {
                return await _apiFootballClient.GetTeamsDataAsync(countryName, sortByTeamName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve football data for country {CountryName}", countryName);
                return null;
            }
        }
        public static (string? countryCode, string? countryName) ParseCountryInput(string? country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return (null, null);
            
            // If country input is a two-letter code, try using RegionInfo directly
            if (country.Length == 2)
            {
                try
                {
                    var region = new RegionInfo(country.ToUpper());
                    return (region.TwoLetterISORegionName, region.EnglishName);
                }
                catch (ArgumentException)
                {
                    // Log warning if the country code is invalid
                    Log.Warning("Invalid country code '{Country}' provided.", country);
                }
            }
            
            // Search through all specific cultures to find a matching region by name
            var regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(culture => new RegionInfo(culture.Name))
                .DistinctBy(region => region.Name);
            
            var matchedRegion = regions.FirstOrDefault(region =>
            region.EnglishName.Equals(country, StringComparison.OrdinalIgnoreCase) ||
            region.TwoLetterISORegionName.Equals(country, StringComparison.OrdinalIgnoreCase));
            
            if (matchedRegion != null)
            {
                return (matchedRegion.TwoLetterISORegionName, matchedRegion.EnglishName);
            }
            
            // Log warning if no match found
            Log.Warning("Country '{Country}' not recognized.", country);
            return (null, country); 
        }
    }
}

