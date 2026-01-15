using Microsoft.Extensions.Configuration;
using MyShop.Contracts;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyShop.Services
{
    public class ConfigService : IConfigService
    {
        private const string ServerAddressKey = "ServerAddress";
        private readonly string _defaultAddress;
        private readonly ApplicationDataContainer _localSettings;

        public ConfigService(IConfiguration configuration)
        {
            _defaultAddress = configuration["GraphQL:Endpoint"] ?? "http://localhost:4000";
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        public string GetServerAddress()
        {
            var savedAddress = _localSettings.Values[ServerAddressKey] as string;
            
            if (string.IsNullOrWhiteSpace(savedAddress))
            {
                Debug.WriteLine($"[CONFIG] No saved address, using default: {_defaultAddress}");
                return _defaultAddress;
            }

            Debug.WriteLine($"[CONFIG] Using saved address: {savedAddress}");
            return savedAddress;
        }

        public void SetServerAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.WriteLine("[CONFIG] Clearing saved address");
                _localSettings.Values.Remove(ServerAddressKey);
            }
            else
            {
                Debug.WriteLine($"[CONFIG] Saving address: {address}");
                _localSettings.Values[ServerAddressKey] = address.Trim();
            }
        }

        public string GetDefaultServerAddress()
        {
            return _defaultAddress;
        }

        public async Task<bool> TestConnectionAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            try
            {
                Debug.WriteLine($"[CONFIG] Testing connection to: {address}");

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                // GraphQL introspection query
                var query = "{\"query\":\"{__typename}\"}";
                var content = new StringContent(query, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(address, content);

                var isSuccess = response.IsSuccessStatusCode;
                Debug.WriteLine($"[CONFIG] Connection test: {(isSuccess ? "✓ Success" : "✗ Failed")}");

                return isSuccess;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CONFIG] Connection test error: {ex.Message}");
                return false;
            }
        }
    }
}
