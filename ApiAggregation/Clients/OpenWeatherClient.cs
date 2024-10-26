namespace ApiAggregation.Clients
{
    public class OpenWeatherClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "8a937d73802006b74ec8384472056e32";

        public OpenWeatherClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<object> GetDataAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<object>(
                $"https://api.openweathermap.org/data/2.5/weather?q=London&appid={_apiKey}");
            return response;
        }


    }
}
