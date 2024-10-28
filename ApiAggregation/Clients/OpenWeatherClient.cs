using ApiAggregation.Services;
using ApiAggregation.Utilities;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace ApiAggregation.Clients
{
    public class OpenWeatherClient: IOpenWeatherClient
    {
        private readonly HttpClient _httpClient;
        private readonly CacheService _cacheService;
        private readonly string _apiKey = "8a937d73802006b74ec8384472056e32";
        private const int MaxRetries = 3;
        private readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);
        private readonly ApiStatisticsService _statisticsService;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);



        public OpenWeatherClient(HttpClient httpClient, ApiStatisticsService statisticsService, CacheService cacheService)
        {
            _httpClient = httpClient;
            _statisticsService = statisticsService;
            _cacheService = cacheService;

        }

        public async Task<object> GetDataAsync(string? city)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                ValidateCityInput(city);
                // Check if data is already cached
                var cacheKey = $"weather_{city?.ToLower()}";
                // Try to retrieve data from the cache. 
                var cachedData = _cacheService.Get<object>(cacheKey);

                if (cachedData != null)
                {
                    //We skip the API call
                    _statisticsService.TrackRequest("OpenWeather", 0); // Cached requests are near-zero in time
                    return cachedData;
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


                        stopwatch.Stop();
                        _statisticsService.TrackRequest("OpenWeather", stopwatch.Elapsed.TotalMilliseconds);

                        // Check if content is available before attempting to parse
                        var jsonString = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(jsonString))
                        {
                            Log.Warning("Received empty JSON response for city: {City}", city);
                            return FallbackUtilites.GetWeatherFallback("Weather data currently unavailable.");
                        }
                        // Attempt to deserialize JSON content
                        var deserializedResponse = JsonSerializer.Deserialize<object>(jsonString);
                        if (deserializedResponse == null)
                        {
                            Log.Warning("Deserialized response is null for city: {City}. Returning fallback data.", city);
                            return FallbackUtilites.GetWeatherFallback("Weather data currently unavailable.");
                        }

                        // Store the API called data
                        _cacheService.Set(cacheKey, deserializedResponse, CacheExpiration);

                        return deserializedResponse;
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Log.Warning("City '{City}' not found in the weather API.", city);
                        stopwatch.Stop();
                        _statisticsService.TrackRequest("OpenWeather", stopwatch.Elapsed.TotalMilliseconds);
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
                            stopwatch.Stop();
                            _statisticsService.TrackRequest("OpenWeather", stopwatch.Elapsed.TotalMilliseconds);
                            return FallbackUtilites.GetWeatherFallback("Unable to fetch weather data after retries.");
                        }

                        await Task.Delay(delay);
                        delay *= 2;
                    }
                }

            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Invalid city name provided: {City}", city);
                stopwatch.Stop();
                _statisticsService.TrackRequest("OpenWeather", stopwatch.Elapsed.TotalMilliseconds);
                return FallbackUtilites.GetWeatherFallback("Invalid city name provided.");
            }

            // Return fallback in case of unknown failure
            stopwatch.Stop();
            _statisticsService.TrackRequest("OpenWeather", stopwatch.Elapsed.TotalMilliseconds);
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
