using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyShop.Services.GraphQL
{
    public class GraphQLClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _graphqlEndpoint;
        private readonly JsonSerializerOptions _jsonOptions;

        public GraphQLClient(string graphqlEndpoint)
        {
            _graphqlEndpoint = graphqlEndpoint ?? "http://localhost:4000";
            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            Debug.WriteLine($"[GraphQL] Client initialized with endpoint: {_graphqlEndpoint}");
        }

        public async Task<T?> QueryAsync<T>(string query, object? variables = null, string? token = null)
        {
            // For direct queries, don't wrap in request object
            string requestBody;
            if (variables == null)
            {
                requestBody = JsonSerializer.Serialize(new { query }, _jsonOptions);
            }
            else
            {
                requestBody = JsonSerializer.Serialize(new { query, variables }, _jsonOptions);
            }

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var fullUrl = $"{_graphqlEndpoint}/graphql";
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[GraphQL] HTTP REQUEST");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[GraphQL] URL: POST {fullUrl}");
                Debug.WriteLine($"[GraphQL] Content-Type: application/json");
                if (!string.IsNullOrEmpty(token))
                {
                    Debug.WriteLine($"[GraphQL] Authorization: Bearer {token.Substring(0, Math.Min(20, token.Length))}...");
                }
                Debug.WriteLine("");
                Debug.WriteLine("[GraphQL] Request Body:");
                Debug.WriteLine(requestBody);
                Debug.WriteLine("════════════════════════════════════════");

                var response = await _httpClient.PostAsync(fullUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();
                
                Debug.WriteLine("");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[GraphQL] HTTP RESPONSE");
                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine($"[GraphQL] Status Code: {response.StatusCode}");
                Debug.WriteLine("");
                Debug.WriteLine("[GraphQL] Response Body:");
                Debug.WriteLine(responseContent);
                Debug.WriteLine("════════════════════════════════════════");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[GraphQL] HTTP Error: {response.StatusCode}");
                    throw new Exception($"HTTP {response.StatusCode}: {responseContent}");
                }

                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                // Check for GraphQL errors
                if (root.TryGetProperty("errors", out var errors))
                {
                    var errorText = errors.GetRawText();
                    Debug.WriteLine($"[GraphQL] GraphQL Errors: {errorText}");
                    throw new Exception($"GraphQL Error: {errorText}");
                }

                // Extract data
                if (root.TryGetProperty("data", out var data))
                {
                    var dataText = data.GetRawText();
                    Debug.WriteLine($"[GraphQL] Data received and parsed");

                    var result = JsonSerializer.Deserialize<T>(dataText, _jsonOptions);
                    return result;
                }

                Debug.WriteLine("[GraphQL] No 'data' property in response");
                return default;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[GraphQL] Connection Error: {ex.Message}");
                Debug.WriteLine($"[GraphQL] Endpoint: {_graphqlEndpoint}");
                Debug.WriteLine("[GraphQL] Make sure GraphQL backend is running!");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GraphQL] Exception: {ex.GetType().Name}");
                Debug.WriteLine($"[GraphQL] Message: {ex.Message}");
                throw;
            }
        }

        public async Task<T?> MutateAsync<T>(string query, object? variables = null, string? token = null)
        {
            return await QueryAsync<T>(query, variables, token);
        }
    }
}   