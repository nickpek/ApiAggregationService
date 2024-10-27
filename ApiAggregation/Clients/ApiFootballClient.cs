namespace ApiAggregation.Clients
{
    using Serilog;
    using System.Net.Http.Headers;
    using System.Text.Json;

    namespace ApiAggregation.Clients
    {
        public class ApiFootballClient
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

                        // Extract "response" array containing the teams
                        var teams = root.GetProperty("response");

                        // Apply sorting if requested
                        if (sortByName)  
                            teams = SortJsonArrayByName(teams);

                        // Reconstruct the JSON structure with the sorted "response"
                        var sortedRootJson = JsonSerializer.Serialize(new { response = teams });
                        using var sortedRootDoc = JsonDocument.Parse(sortedRootJson);
                        return sortedRootDoc.RootElement.Clone();
                    }
                    catch (HttpRequestException ex)
                    {
                        retryCount++;
                        Log.Warning(ex, "Attempt {RetryCount} to fetch teams data for country {Country} failed", retryCount, country);

                        // Check if retries have been exhausted
                        if (retryCount >= MaxRetries)
                        {
                            Log.Error(ex, "Max retries reached. Unable to fetch teams data for country {Country}", country);
                            return GetTeamsFallback("Unable to fetch teams data after retries.");
                        }

                        // Exponential backoff for retries
                        await Task.Delay(delay);
                        delay *= 2;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error occurred while fetching teams data for country {Country}", country);
                        return GetTeamsFallback("Unexpected error occurred.");
                    }
                }

                Log.Error("Unknown error: Unable to fetch teams data for country {Country}", country);
                return GetTeamsFallback("Unknown error.");
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

            private static JsonElement GetTeamsFallback(string reason)
            {
                var fallbackJson = $"{{ \"status\": \"error\", \"message\": \"{reason}\" }}";
                using var jsonDocument = JsonDocument.Parse(fallbackJson);
                return jsonDocument.RootElement.Clone();
            }
        }
    }
}
