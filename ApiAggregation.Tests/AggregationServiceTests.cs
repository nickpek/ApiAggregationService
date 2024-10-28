using System.Text.Json;
using Moq;
using ApiAggregation.Services;
using ApiAggregation.Clients;


namespace ApiAggregation.Tests
{
    public class AggregationServiceTests
    {
        private readonly Mock<IOpenWeatherClient> _mockWeatherClient;
        private readonly Mock<INewsApiClient> _mockNewsClient;
        private readonly Mock<IApiFootballClient> _mockFootballClient;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ApiStatisticsService> _mockStatisticsService;

        public AggregationServiceTests()
        {
            // Mock each API client and additional services
            _mockWeatherClient = new Mock<IOpenWeatherClient>();
            _mockNewsClient = new Mock<INewsApiClient>();
            _mockFootballClient = new Mock<IApiFootballClient>();
            _mockCacheService = new Mock<ICacheService>();
            _mockStatisticsService = new Mock<ApiStatisticsService>();

            // Setup mock successful responses for each client
            _mockWeatherClient
                .Setup(client => client.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new { city = "TestCity", temperature = 25 });

            _mockNewsClient
                .Setup(client => client.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new { status = "ok", articles = new[] { new { title = "Sample News" } } });

            _mockFootballClient
                .Setup(client => client.GetTeamsDataAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(JsonDocument.Parse("{\"status\":\"ok\",\"teams\":[{\"name\":\"Team A\"}]}").RootElement);
        }

        [Fact]
        public async Task GetAggregatedDataAsync_ShouldReturnDataFromAllApis_WhenAllApisSucceed()
        {
            // Arrange: Mock responses with corrected JSON structure and proper quoting
            var weatherJson = @"
            {
                ""coord"": { ""lon"": 22.9439, ""lat"": 40.6403 },
                ""main"": { ""temp"": 288.99, ""humidity"": 59 },
                ""name"": ""Thessaloniki""
            }";
            _mockWeatherClient
                .Setup(client => client.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(JsonDocument.Parse(weatherJson).RootElement);

            var newsJson = @"
            {
                ""status"": ""ok"",
                ""totalResults"": 1,
                ""articles"": [
                    { ""source"": { ""name"": ""TestSource"" }, ""title"": ""Test Article"" }
                ]
            }";
            _mockNewsClient
                .Setup(client => client.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync(JsonDocument.Parse(newsJson).RootElement);

            var footballJson = @"
            {
                ""status"": ""ok"",
                ""response"": [
                    { ""team"": { ""name"": ""Team A"", ""id"": 1, ""country"": ""Greece"" } }
                ]
            }";
            _mockFootballClient
                .Setup(client => client.GetTeamsDataAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(JsonDocument.Parse(footballJson).RootElement);

            var service = new AggregationService(_mockWeatherClient.Object, _mockNewsClient.Object, _mockFootballClient.Object);

            // Act
            var result = await service.GetAggregatedDataAsync(city: "Thessaloniki", country: "Greece");

            // Assert: Check presence of key properties in each API response
            Assert.True(result.ContainsKey("weather"), "Expected 'weather' key is missing.");
            Assert.True(result.ContainsKey("news"), "Expected 'news' key is missing.");
            Assert.True(result.ContainsKey("football"), "Expected 'football' key is missing.");

            // Weather structure verification
            var weatherData = (JsonElement)result["weather"];
            Assert.True(weatherData.TryGetProperty("coord", out _), "Weather data missing 'coord' property.");
            Assert.True(weatherData.TryGetProperty("main", out _), "Weather data missing 'main' property.");
            Assert.True(weatherData.TryGetProperty("name", out _), "Weather data missing 'name' property.");

            // News structure verification
            var newsData = JsonSerializer.Deserialize<JsonElement>(result["news"].ToString());
            Assert.True(newsData.TryGetProperty("status", out _), "News data missing 'status' property.");
            Assert.True(newsData.TryGetProperty("articles", out var articles), "News data missing 'articles' property.");
            Assert.True(articles.GetArrayLength() > 0, "Expected at least one article in 'articles' array.");

            // Football structure verification
            var footballData = JsonSerializer.Deserialize<JsonElement>(result["football"].ToString());
            Assert.True(footballData.TryGetProperty("status", out _), "Football data missing 'status' property.");
            Assert.True(footballData.TryGetProperty("response", out var response), "Football data missing 'response' property.");
            Assert.True(response.GetArrayLength() > 0, "Expected at least one team in 'response' array.");

        }

        //handles a failure in one API (e.g., football) while still returning data from the other APIs successfully,
        //with a fallback structure for the failed API.
        [Fact]
        public async Task GetAggregatedDataAsync_ShouldHandleOneApiFailure_AndReturnDataForOthers()
        {
            // Arrange: Simulate a failure in the football API and success in weather and news APIs
            _mockFootballClient
                .Setup(client => client.GetTeamsDataAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(JsonDocument.Parse(@"{ ""status"": ""error"", ""message"": ""Teams data currently unavailable."", ""response"": []}").RootElement);

            _mockWeatherClient
               .Setup(client => client.GetDataAsync(It.IsAny<string>()))
               .ReturnsAsync(JsonDocument.Parse(@"{ ""coord"": { ""lon"": 22.9439, ""lat"": 40.6403 }, ""main"": { ""temp"": 288.99, ""humidity"": 59 }, ""name"": ""Thessaloniki"" }").RootElement);

            _mockNewsClient
               .Setup(client => client.GetDataAsync(It.IsAny<string>()))
               .ReturnsAsync(JsonDocument.Parse(@"{ ""status"": ""ok"", ""articles"": [{ ""source"": { ""name"": ""TestSource"" }, ""title"": ""Test News Article"" }] }").RootElement);

            var service = new AggregationService(_mockWeatherClient.Object, _mockNewsClient.Object, _mockFootballClient.Object);

            // Act
            var result = await service.GetAggregatedDataAsync(city: "Thessaloniki", country: "Greece");

            // Assert: Weather and News succeed, Football has fallback structure
            Assert.True(result.ContainsKey("weather"), "Expected 'weather' key is missing.");
            Assert.True(result.ContainsKey("news"), "Expected 'news' key is missing.");
            Assert.True(result.ContainsKey("football"), "Expected 'football' key is missing.");

            // Verify Weather structure
            var weatherData = JsonSerializer.Deserialize<JsonElement>(result["weather"].ToString());
            Assert.True(weatherData.TryGetProperty("coord", out _), "Expected 'coord' property is missing in 'weather'.");
            Assert.True(weatherData.TryGetProperty("main", out _), "Expected 'main' property is missing in 'weather'.");
            Assert.True(weatherData.TryGetProperty("name", out _), "Expected 'name' property is missing in 'weather'.");

            // Verify News structure
            var newsData = JsonSerializer.Deserialize<JsonElement>(result["news"].ToString());
            Assert.True(newsData.TryGetProperty("status", out _), "Expected 'status' property is missing in 'news'.");
            Assert.True(newsData.TryGetProperty("articles", out var articles), "Expected 'articles' property is missing in 'news'.");
            Assert.True(articles.GetArrayLength() > 0, "Expected 'articles' array in 'news' should have at least one item.");

            // Verify Football error structure (fallback data)
            var footballData = JsonSerializer.Deserialize<JsonElement>(result["football"].ToString());
            Assert.True(footballData.TryGetProperty("status", out var status), "Expected 'status' property is missing in 'football'.");
            Assert.Equal("error", status.GetString());
            Assert.True(footballData.TryGetProperty("message", out _), "Expected 'message' property is missing in 'football'.");

        }
    }
}
