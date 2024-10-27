using ApiAggregation.Utilities;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ApiAggregation.Clients
{
    public class ApiFootballClient : IApiFootballClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const int MaxRetries = 3;
        private readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);

        public ApiFootballClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["ApiKeys:ApiFootball"]
                      ?? throw new ArgumentNullException("API key for Api-Football is missing.");

            _httpClient.DefaultRequestHeaders.Add("x-apisports-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<object> GetTeamsDataAsync(string? country, bool sortByName = true)
        {
            try
            {
                // Validate input with error handling
                ValidateCountryInput(country);
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Invalid country code provided: {Country}", country);
                return FallbackUtilites.GetTeamsFallback("Invalid country code provided.");
            }

            int retryCount = 0;
            TimeSpan delay = InitialDelay;

            while (retryCount < MaxRetries)
            {
                try
                {
                    // Construct URL with the country filter
                    var url = $"https://v3.football.api-sports.io/teams?country={country}";

                    // Fetch data
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    // Parse JSON response
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var deserializedResponse = JsonSerializer.Deserialize<object>(jsonString);

                    if (deserializedResponse == null)
                    {
                        Log.Warning("Deserialized response is null, returning fallback data.");
                        return FallbackUtilites.GetTeamsFallback("Teams data currently unavailable.");
                    }

                    // Sort the teams if requested
                    if (sortByName)
                    {
                        var rootElement = JsonDocument.Parse(jsonString).RootElement;
                        var teams = SortJsonArrayByName(rootElement.GetProperty("response"));
                        var sortedRootJson = JsonSerializer.Serialize(new { response = teams });
                        return JsonDocument.Parse(sortedRootJson).RootElement.Clone();
                    }

                    return deserializedResponse;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Warning("Country '{Country}' not found in the football API.", country);
                    return FallbackUtilites.GetTeamsFallback("Country not found in the football API.");
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Log.Warning(ex, "Error fetching teams data for country '{Country}' on attempt {RetryCount}", country, retryCount);

                    if (retryCount >= MaxRetries)
                    {
                        Log.Error("Max retries reached. Returning fallback data for teams API.");
                        return FallbackUtilites.GetTeamsFallback("Unable to fetch teams data after retries.");
                    }

                    await Task.Delay(delay);
                    delay *= 2;
                }
            }

            return FallbackUtilites.GetTeamsFallback("Unexpected error occurred.");
        }

        private static JsonElement SortJsonArrayByName(JsonElement teams)
        {
            // Sort the JSON array by the "name" property within each "team" object
            var sortedTeams = teams.EnumerateArray()
                .OrderBy(team => team.GetProperty("team").GetProperty("name").GetString())
                .ToArray();

            // Reparse sorted array to JsonDocument and return as JsonElement
            using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(sortedTeams));
            return jsonDocument.RootElement.Clone();
        }
        private static void ValidateCountryInput(string? country)
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                Log.Warning("Country name validation failed. Provided country name is null or whitespace.");
                throw new ArgumentException("Country code must be provided.", nameof(country));
            }
        }


    }

}
