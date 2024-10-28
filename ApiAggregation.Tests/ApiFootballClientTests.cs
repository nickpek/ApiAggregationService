using Moq.Protected;
using Moq;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Text;
using ApiAggregation.Clients;
using ApiAggregation.Services;
using ApiAggregation.Utilities;
using System.Text.Json;


namespace ApiAggregation.Tests
{
    public class ApiFootballClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _mockHttpClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ICacheService> _mockCacheService;

        private readonly Mock<ApiStatisticsService> _mockStatisticsService;


        public ApiFootballClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(config => config["ApiKeys:ApiFootball"]).Returns("fake-api-key");
            _mockCacheService = new Mock<ICacheService>();
            _mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockStatisticsService = new Mock<ApiStatisticsService>(); // Mock for statistics service

        }
        // Test for maximum retry fallback
        [Fact]
        public async Task GetTeamsDataAsync_ShouldReturnFallbackData_WhenMaxRetriesExceeded()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Simulated network error"));

            var client = new ApiFootballClient(_mockHttpClient, _mockConfiguration.Object, _mockStatisticsService.Object, _mockCacheService.Object);
            var country = "ValidCountry";

            // Act
            var result = await client.GetTeamsDataAsync(country);

            //// Assert fallback structure directly
            //var fallback = JsonSerializer.Serialize(FallbackUtilites.GetTeamsFallback("Unable to fetch teams data after retries."));
            //var actualResult = JsonSerializer.Serialize(result);

            //Assert.Equal(fallback, actualResult);
            // Assert fallback response due to retries being exhausted
            var expectedFallback = FallbackUtilites.GetTeamsFallback("Unable to fetch teams data after retries.");
            Assert.Equal(JsonSerializer.Serialize(expectedFallback), JsonSerializer.Serialize(result));
        }

        // Test for fallback on unexpected errors
        [Fact]
        public async Task GetTeamsDataAsync_ShouldReturnFallbackData_OnUnexpectedError()
        {
            // Arrange - Setup handler to throw a simulated unexpected error
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new Exception("Simulated unexpected error"));

            var client = new ApiFootballClient(_mockHttpClient, _mockConfiguration.Object, _mockStatisticsService.Object, _mockCacheService.Object);
            var country = "ValidCountry";

            // Act
            var result = await client.GetTeamsDataAsync(country);

            var expectedFallback = FallbackUtilites.GetTeamsFallback("Unexpected error occurred.");
            Assert.Equal(JsonSerializer.Serialize(expectedFallback), JsonSerializer.Serialize(result));
        }

        // Test for successful data retrieval
        [Fact]
       public async Task GetTeamsDataAsync_ShouldReturnData_OnSuccessfulRequest()
        {
            // Arrange - Simulate a successful response with the actual JSON structure
            var sampleJson = @"{
                ""response"": [
                  {
                    ""team"": {
                      ""id"": 553,
                      ""name"": ""Olympiakos Piraeus"",
                      ""code"": ""OLY"",
                      ""country"": ""Greece"",
                      ""founded"": 1925,
                      ""national"": false,
                      ""logo"": ""https://media.api-sports.io/football/teams/553.png""
                    },
                    ""venue"": {
                      ""id"": 775,
                      ""name"": ""Stadio Georgios Karaiskáki"",
                      ""address"": ""Poseidonos Avenue, Faliro"",
                      ""city"": ""Piraeus"",
                      ""capacity"": 33296,
                      ""surface"": ""grass"",
                      ""image"": ""https://media.api-sports.io/football/venues/775.png""
                    }
                  }
                ]
            }";
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
                });

            var client = new ApiFootballClient(_mockHttpClient, _mockConfiguration.Object, _mockStatisticsService.Object, _mockCacheService.Object);
            var country = "Greece";
            
            // Act
            var result = await client.GetTeamsDataAsync(country, sortByName: true);


            // Validate the team data
            Assert.NotNull(result);
            Assert.Contains("Olympiakos Piraeus", result.ToString());
            Assert.Contains("Greece", result.ToString());
        }

        //Uses different status codes to ensure that it returns relevant fallback messages
        //based on specific HTTP status codes (like NotFound or InternalServerError)
        [Theory]
        [InlineData(HttpStatusCode.NotFound, "Country not found in the football API.")]
        [InlineData(HttpStatusCode.InternalServerError, "Unable to fetch teams data after retries.")]
        public async Task GetTeamsDataAsync_ShouldReturnAppropriateFallbackData_OnStatusCode(HttpStatusCode statusCode, string expectedMessage)
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode
                });

            var client = new ApiFootballClient(_mockHttpClient, _mockConfiguration.Object, _mockStatisticsService.Object, _mockCacheService.Object);
            var country = "NonexistentCountry";

            // Act
            var result = await client.GetTeamsDataAsync(country);

            // Assert
            var expectedFallback = FallbackUtilites.GetTeamsFallback(expectedMessage);
            Assert.Equal(JsonSerializer.Serialize(expectedFallback), JsonSerializer.Serialize(result));
        }
    }
}
