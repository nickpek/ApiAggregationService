using System.Text.Json;

namespace ApiAggregation.Clients
{
    public class NewsApiClient 
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "289d0bae47784394a56c52b21155d647";
        private const int MaxRetries = 3;
        private readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);

        public NewsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object> GetDataAsync(string? country = null)
        {
            try
            {
                // Validate input with error handling
                ValidateCountryInput(country);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message); // Log or handle validation exception
                return GetNewsFallback("Invalid country code provided.");
            }

            int retryCount = 0;
            TimeSpan delay = InitialDelay;

            while (retryCount < MaxRetries)
            {
                try
                {
                    // Construct the base URL
                    var url = $"https://newsapi.org/v2/top-headlines";

                    // Append query parameters
                    if (!string.IsNullOrEmpty(country))
                    {
                        url += $"?country={country}&apiKey={_apiKey}";
                    }
                    else
                    {
                        url += $"?apiKey={_apiKey}";
                    }                   

                    // Prepare the request with necessary headers
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("User-Agent", "Mozilla/5.0");
                    request.Headers.Add("Accept", "application/json");

                    // Send the request and get the response
                    using var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    // Parse JSON response
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(jsonString);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Handle specific status codes, e.g., Not Found
                    Console.WriteLine($"Country '{country}' not found in the news API.");
                    return GetNewsFallback("Country not found in the news API.");
                }
                catch (Exception ex)
                {
                    retryCount++;

                    // Log the error
                    Console.WriteLine($"Error fetching news data for country '{country}': {ex.Message}");

                    // Check if retries have been exhausted
                    if (retryCount >= MaxRetries)
                    {
                        Console.WriteLine("Max retries reached. Returning fallback data for news API.");
                        return GetNewsFallback("Unable to fetch news data after retries.");
                    }

                    // Wait before retrying (exponential backoff)
                    await Task.Delay(delay);
                    delay *= 2;
                }
            }

            // Return fallback in case of unknown failure
            return GetNewsFallback("Unexpected error occurred.");
        }

        private static void ValidateCountryInput(string? country)
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                throw new ArgumentException("Country code must be provided.", nameof(country));
            }
        }

        private static object GetNewsFallback(string reason)
        {
            return new
            {
                status = "error",
                message = reason,
                articles = new[]
                {
                    new
                    {
                        source = new { id = "fallback", name = "Fallback News Source" },
                        author = "N/A",
                        title = "News data currently unavailable.",
                        description = "Please try again later.",
                        url = "#",
                        urlToImage = "#",
                        publishedAt = DateTime.UtcNow,
                        content = "N/A"
                    }
                }
            };
        }
    }
}
