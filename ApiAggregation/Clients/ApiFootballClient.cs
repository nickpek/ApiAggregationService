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

        public async Task<JsonElement> GetTeamsDataAsync(string country, bool sortByName = true)
        {
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

                    // Parse JSON response as JsonDocument
                    using var jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var root = jsonDocument.RootElement;

                    var teams = root.GetProperty("response");

                    // Apply sorting if requested
                    if (sortByName)
                        teams = SortJsonArrayByName(teams);

                    // Reconstruct the JSON structure with the sorted "response"
                    var sortedRootJson = JsonSerializer.Serialize(new { response = teams });
                    using var sortedRootDoc = JsonDocument.Parse(sortedRootJson);
                    return sortedRootDoc.RootElement.Clone();
                }
                catch (HttpRequestException ex) when (retryCount < MaxRetries - 1)
                {
                    retryCount++;
                    Log.Warning(ex, "Attempt {RetryCount} to fetch teams data for country {Country} failed", retryCount, country);

                    // Exponential backoff for retries
                    await Task.Delay(delay);
                    delay *= 2;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Error("Country not found in the API.");
                    return FallbackUtilites.GetTeamsFallback("Country not found in the API.");
                }
                catch (HttpRequestException)
                {
                    Log.Error("Max retries reached. Unable to fetch teams data.");
                    return FallbackUtilites.GetTeamsFallback("Unable to fetch teams data after retries.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error occurred while fetching teams data for country {Country}", country);
                    return FallbackUtilites.GetTeamsFallback("Unexpected error occurred.");
                }
            }

            Log.Error("Unknown error: Unable to fetch teams data for country {Country}", country);
            return FallbackUtilites.GetTeamsFallback("Unknown error.");
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

      
    }

}
