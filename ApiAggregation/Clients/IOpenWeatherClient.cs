namespace ApiAggregation.Clients
{
    public interface IOpenWeatherClient
    {
        Task<object> GetDataAsync(string? city);
    }

}