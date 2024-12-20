﻿using ApiAggregation.Services;
using ApiAggregation.Utilities;
using Serilog;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace ApiAggregation.Clients
{
    public class ApiFootballClient : IApiFootballClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly string _apiKey;
        private const int MaxRetries = 3;
        private readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);
        private readonly ApiStatisticsService _statisticsService;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public ApiFootballClient(HttpClient httpClient, IConfiguration configuration, ApiStatisticsService statisticsService, ICacheService cacheService)
        {
            _httpClient = httpClient;
            _statisticsService = statisticsService;
            _cacheService = cacheService;

            _apiKey = configuration["ApiKeys:ApiFootball"]
                      ?? throw new ArgumentNullException("API key for Api-Football is missing.");

            _httpClient.DefaultRequestHeaders.Add("x-apisports-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<object> GetTeamsDataAsync(string? country, bool sortByName = true)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate input with error handling
                ValidateCountryInput(country);

                var cacheKey = $"football_teams_{country?.ToLower()}_{sortByName}";
                var cachedData = _cacheService.Get<object>(cacheKey);

                if (cachedData != null)
                {
                    _statisticsService.TrackRequest("ApiFootball", 0); // Cached requests are near-zero in time
                    return cachedData;
                }

                int retryCount = 0;
                TimeSpan delay = InitialDelay;

                while (retryCount < MaxRetries)
                {
                    try
                    {
                        var url = $"https://v3.football.api-sports.io/teams?country={country}";
                        var response = await _httpClient.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        var jsonString = await response.Content.ReadAsStringAsync();
                        var deserializedResponse = JsonSerializer.Deserialize<object>(jsonString);

                        if (deserializedResponse == null)
                        {
                            Log.Warning("Deserialized response is null, returning fallback data.");
                            _statisticsService.TrackRequest("ApiFootball", stopwatch.Elapsed.TotalMilliseconds);
                            return FallbackUtilites.GetTeamsFallback("Teams data currently unavailable.");
                        }

                        if (sortByName)
                        {
                            var rootElement = JsonDocument.Parse(jsonString).RootElement;
                            var teams = SortJsonArrayByName(rootElement.GetProperty("response"));
                            var sortedRootJson = JsonSerializer.Serialize(new { response = teams });
                            _statisticsService.TrackRequest("ApiFootball", stopwatch.Elapsed.TotalMilliseconds);
                            return JsonDocument.Parse(sortedRootJson).RootElement.Clone();
                        }

                        stopwatch.Stop();
                        _statisticsService.TrackRequest("ApiFootball", stopwatch.Elapsed.TotalMilliseconds);
                        _cacheService.Set(cacheKey, deserializedResponse, TimeSpan.FromMinutes(10));
                        return deserializedResponse;
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        Log.Warning("Country '{Country}' not found in the football API.", country);
                        stopwatch.Stop();
                        _statisticsService.TrackRequest("ApiFootball", stopwatch.Elapsed.TotalMilliseconds);
                        return FallbackUtilites.GetTeamsFallback("Country not found in the football API.");
                    }
                    catch (HttpRequestException ex)
                    {
                        retryCount++;
                        Log.Warning(ex, "Error fetching teams data for country '{Country}' on attempt {RetryCount}", country, retryCount);

                        if (retryCount >= MaxRetries)
                        {
                            Log.Error("Max retries reached. Returning fallback data for teams API.");
                            stopwatch.Stop();
                            _statisticsService.TrackRequest("ApiFootball", stopwatch.Elapsed.TotalMilliseconds);
                            return FallbackUtilites.GetTeamsFallback("Unable to fetch teams data after retries.");
                        }

                        await Task.Delay(delay);
                        delay *= 2;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Invalid country code provided: {Country}", country);
                stopwatch.Stop();
                _statisticsService.TrackRequest("ApiFootball", stopwatch.Elapsed.TotalMilliseconds);
                return FallbackUtilites.GetTeamsFallback("Invalid country code provided.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error occurred while fetching teams data for country '{Country}'.", country);
                stopwatch.Stop();
                _statisticsService.TrackRequest("ApiFootball", stopwatch.Elapsed.TotalMilliseconds);
                return FallbackUtilites.GetTeamsFallback("Unexpected error occurred.");
            }

            // Return fallback in case of unknown failure
            stopwatch.Stop();
            _statisticsService.TrackRequest("ApiFootball", stopwatch.Elapsed.TotalMilliseconds);
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
