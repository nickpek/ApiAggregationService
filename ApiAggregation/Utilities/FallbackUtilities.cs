using System.Text.Json;

namespace ApiAggregation.Utilities
{
    public static class FallbackUtilites
    {
        public static object GetWeatherFallback(string reason)
        {
            return new
            {
                city = "Unknown",
                description = reason,
                temperature = "N/A",
                humidity = "N/A"
            };
        }

        public static object GetNewsFallback(string reason)
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

        public static JsonElement GetTeamsFallback(string reason)
        {
            var fallbackJson = $"{{ \"status\": \"error\", \"message\": \"{reason}\" }}";
            using var jsonDocument = JsonDocument.Parse(fallbackJson);
            return jsonDocument.RootElement.Clone();
        }
    }

}
