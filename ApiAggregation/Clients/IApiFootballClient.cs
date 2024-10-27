using System.Text.Json;

namespace ApiAggregation.Clients
{
    public interface IApiFootballClient
    {
        Task<JsonElement> GetTeamsDataAsync(string country, bool sortByName = true);
    }
}