using ApiAggregation.Clients;
using ApiAggregation.Clients.ApiAggregation.Clients;
using Serilog;
using System.Globalization;

namespace ApiAggregation.Services
{
    public class AggregationService
    {
        private readonly OpenWeatherClient _openWeatherClient;
        private readonly NewsApiClient _newsApiClient;
        private readonly ApiFootballClient _apiFootballClient;


        public AggregationService(OpenWeatherClient openWeatherClient, NewsApiClient newsApiClient, ApiFootballClient apiFootballClient)
        {
            _openWeatherClient = openWeatherClient;
            _newsApiClient = newsApiClient;
            _apiFootballClient = apiFootballClient;

        }
        public async Task<Dictionary<string, object>> GetAggregatedDataAsync(string? city = null, string? country = null, bool sortByTeamName = true)
        {
            var result = new Dictionary<string, object>();

            var (countryCode, countryName) = ParseCountryInput(country);

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
                var newsData = await _newsApiClient.GetDataAsync(countryCode);
                result.Add("news", newsData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve news data for country {Country}", countryCode);
            }
            // Fetch football teams data with country name for filtering and sorting by team name if specified
            if (!string.IsNullOrEmpty(countryName))
            {
                try
                {
                    var teamsData = await _apiFootballClient.GetTeamsDataAsync(countryName, sortByTeamName);
                    result.Add("football", teamsData);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to retrieve football data for country {CountryName}", countryName);
                }
            }
            else
            {
                Log.Warning("Country name not provided. Skipping football data retrieval.");
            }


            return result;

        }
        private static (string? countryCode, string? countryName) ParseCountryInput(string? country)
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

