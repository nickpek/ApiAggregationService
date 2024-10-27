using Moq.Protected;
using Moq;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Text;
using ApiAggregation.Clients;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiAggregation.Tests
{
    public class ApiFootballClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _mockHttpClient;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public ApiFootballClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(config => config["ApiKeys:ApiFootball"]).Returns("fake-api-key");

            _mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
        }
        //returns fallback data after maximum retry attempts are exhausted due to repeated network errors.
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

            var client = new ApiFootballClient(_mockHttpClient, _mockConfiguration.Object);
            var country = "ValidCountry";

            // Act
            var result = await client.GetTeamsDataAsync(country);

            // Assert
            Assert.Equal("error", result.GetProperty("status").GetString());
            Assert.Equal("Unable to fetch teams data after retries.", result.GetProperty("message").GetString());
        }

        //provides fallback data immediately upon encountering an unexpected error, without retrying
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

            var client = new ApiFootballClient(_mockHttpClient, _mockConfiguration.Object);
            var country = "ValidCountry";

            // Act
            var result = await client.GetTeamsDataAsync(country);

            // Assert - Verify fallback data for an immediate unexpected error
            Assert.Equal("error", result.GetProperty("status").GetString());
            Assert.Equal("Unexpected error occurred.", result.GetProperty("message").GetString());
        }
        //returns team data with a structured response when the API call succeeds.
        [Fact]
        // Test for successful data retrieval with proper JSON structure
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
            
            var client = new ApiFootballClient(_mockHttpClient, _mockConfiguration.Object);
            var country = "Greece";
            
            // Act
            var result = await client.GetTeamsDataAsync(country, sortByName: true);
            
            Assert.True(result.TryGetProperty("response", out var response), "Response property not found in the result JSON.");
            Assert.True(response.GetArrayLength() > 0, "Response array is empty.");
            
            // Validate the team data
            var team = response[0].GetProperty("team");
            Assert.Equal(553, team.GetProperty("id").GetInt32());
            Assert.Equal("Olympiakos Piraeus", team.GetProperty("name").GetString());
            Assert.Equal("OLY", team.GetProperty("code").GetString());
            Assert.Equal("Greece", team.GetProperty("country").GetString());
        }

        //Uses different status codes to ensure that it returns relevant fallback messages
        //based on specific HTTP status codes (like NotFound or InternalServerError)
        [Theory]
        [InlineData(HttpStatusCode.NotFound, "Country not found in the API.")]
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

            var client = new ApiFootballClient(_mockHttpClient, _mockConfiguration.Object);
            var country = "NonexistentCountry";

            // Act
            var result = await client.GetTeamsDataAsync(country);

            // Assert
            Assert.Equal("error", result.GetProperty("status").GetString());
            Assert.Equal(expectedMessage, result.GetProperty("message").GetString());
        }

    }
}
