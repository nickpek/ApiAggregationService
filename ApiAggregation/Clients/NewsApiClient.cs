﻿using ApiAggregation.Services;
using ApiAggregation.Utilities;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace ApiAggregation.Clients
{
    public class NewsApiClient: INewsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly string _apiKey = "289d0bae47784394a56c52b21155d647";
        private const int MaxRetries = 3;
        private readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);
        private readonly ApiStatisticsService _statisticsService;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public NewsApiClient(HttpClient httpClient, ApiStatisticsService statisticsService, ICacheService cacheService)
        {
            _httpClient = httpClient;
            _statisticsService = statisticsService;
            _cacheService = cacheService;
        }

        public async Task<object> GetDataAsync(string? country = null)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate input with error handling
                ValidateCountryInput(country);

                var cacheKey = $"news_{country?.ToLower()}";
                var cachedData = _cacheService.Get<object>(cacheKey);

                if (cachedData != null)
                {
                    //We skip the API call
                    _statisticsService.TrackRequest("NewsApi", 0); // Cached requests are near-zero in time
                    return cachedData;
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

                        stopwatch.Stop();
                        _statisticsService.TrackRequest("NewsApi", stopwatch.Elapsed.TotalMilliseconds);


                        // Parse JSON response
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var deserializedResponse = JsonSerializer.Deserialize<object>(jsonString);
                        if (deserializedResponse == null)
                        {
                            Log.Warning("Deserialized response is null, returning fallback data.");
                            stopwatch.Stop();
                            _statisticsService.TrackRequest("NewsApi", stopwatch.Elapsed.TotalMilliseconds);
                            return FallbackUtilites.GetNewsFallback("News data currently unavailable.");
                        }
                        // Cache the response and return it
                        _cacheService.Set(cacheKey, deserializedResponse, CacheExpiration);
                        return deserializedResponse;
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Handle specific status codes, e.g., Not Found
                        Log.Warning("Country '{Country}' not found in the news API.", country);
                        stopwatch.Stop();
                        _statisticsService.TrackRequest("NewsApi", stopwatch.Elapsed.TotalMilliseconds);
                        return FallbackUtilites.GetNewsFallback("Country not found in the news API.");
                    }
                    catch (Exception ex)
                    {
                        retryCount++;

                        Log.Warning(ex, "Error fetching news data for country '{Country}' on attempt {RetryCount}", country, retryCount);

                        // Check if retries have been exhausted
                        if (retryCount >= MaxRetries)
                        {
                            Log.Error("Max retries reached. Returning fallback data for news API.");
                            stopwatch.Stop();
                            _statisticsService.TrackRequest("NewsApi", stopwatch.Elapsed.TotalMilliseconds);
                            return FallbackUtilites.GetNewsFallback("Unable to fetch news data after retries.");
                        }

                        // Wait before retrying (exponential backoff)
                        await Task.Delay(delay);
                        delay *= 2;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Invalid country code provided: {Country}", country);
                stopwatch.Stop();
                _statisticsService.TrackRequest("NewsApi", stopwatch.Elapsed.TotalMilliseconds);
                return FallbackUtilites.GetNewsFallback("Invalid country code provided.");
            }            

            // Return fallback in case of unknown failure
            stopwatch.Stop();
            _statisticsService.TrackRequest("NewsApi", stopwatch.Elapsed.TotalMilliseconds);
            return FallbackUtilites.GetNewsFallback("Unexpected error occurred.");
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
