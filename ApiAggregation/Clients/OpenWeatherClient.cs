using Serilog;

namespace ApiAggregation.Clients
{
    public class OpenWeatherClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "8a937d73802006b74ec8384472056e32";
        private const int MaxRetries = 3;
        private readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);

        public OpenWeatherClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object> GetDataAsync(string city)
        {
            // Validate the input
            ValidateCityInput(city);

            int retryCount = 0;
            TimeSpan delay = InitialDelay;

            while (retryCount < MaxRetries)
            {
                try
                {
                    // Build the request URL
                    var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}";

                    // Send request
                    var response = await _httpClient.GetAsync(url);

                    // Ensure the request was successful
                    response.EnsureSuccessStatusCode();

                    // Parse JSON response
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Handle specific status codes, e.g., Not Found
                    Console.WriteLine($"City '{city}' not found in the weather API.");
                    return GetWeatherFallback(city, "City not found.");
                }
                catch (Exception ex)
                {
                    retryCount++;

                    Log.Warning(ex, "Error fetching weather data for '{City}' on attempt {RetryCount}", city, retryCount);

                    // Check if retries have been exhausted
                    if (retryCount >= MaxRetries)
                    {
                        Log.Error("Max retries reached. Returning fallback data for city '{City}'", city);
                        return GetWeatherFallback(city, "Unable to fetch weather data after retries.");
                    }

                    await Task.Delay(delay);
                    delay *= 2;
                }
            }

            // Return fallback in case of unknown failure
            return GetWeatherFallback(city, "Unexpected error occurred.");
        }

        private static void ValidateCityInput(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException("City name must be provided.", nameof(city));
            }
        }

        private static object GetWeatherFallback(string city, string reason)
        {
            return new
            {
                city,
                description = reason,
                temperature = "N/A",
                humidity = "N/A"
            };
        }
    }
}
