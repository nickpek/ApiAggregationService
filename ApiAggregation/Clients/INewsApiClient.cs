namespace ApiAggregation.Clients
{
    public interface INewsApiClient
    {
        Task<object> GetDataAsync(string? country);
    }
}