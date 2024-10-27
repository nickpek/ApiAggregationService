using ApiAggregation.Utilities;
using Serilog;

namespace ApiAggregation.Clients
{
    public class OpenWeatherClient: IOpenWeatherClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "8a937d73802006b74ec8384472056e32";
        private const int MaxRetries = 3;
        private readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);

        public OpenWeatherClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object> GetDataAsync(string? city)
        {
            try
            {
                ValidateCityInput(city);
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Invalid city name provided: {City}", city);
                return FallbackUtilites.GetWeatherFallback("Invalid city name provided.");
            }

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
                    Log.Warning("City '{City}' not found in the weather API.", city);
                    return FallbackUtilites.GetWeatherFallback("City not found.");
                }
                catch (Exception ex)
                {
                    retryCount++;

                    Log.Warning(ex, "Error fetching weather data for '{City}' on attempt {RetryCount}", city, retryCount);

                    // Check if retries have been exhausted
                    if (retryCount >= MaxRetries)
                    {
                        Log.Error("Max retries reached. Returning fallback data for city '{City}'", city);
                        return FallbackUtilites.GetWeatherFallback("Unable to fetch weather data after retries.");
                    }

                    await Task.Delay(delay);
                    delay *= 2;
                }
            }

            // Return fallback in case of unknown failure
            return FallbackUtilites.GetWeatherFallback("Unexpected error occurred.");
        }


        private static void ValidateCityInput(string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                Log.Warning("City name validation failed. Provided city name is null or whitespace.");
                throw new ArgumentException("City name must be provided and cannot be empty.", nameof(city));
            }
        }


    }
}
