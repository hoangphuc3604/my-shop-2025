using System.Net.Http;
using System.Threading.Tasks;

namespace MyShop.Services.GraphQL;

public static class GraphQLConnectionTest
{
    public static async Task<bool> TestConnectionAsync(string endpoint)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(endpoint, new StringContent("{\"query\":\"{__typename}\"}", System.Text.Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}