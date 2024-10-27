using System.Text.Json;

namespace ApiAggregation.Clients
{
    public interface IApiFootballClient
    {
        Task<object> GetTeamsDataAsync(string? country, bool sortByName = true);
    }
}