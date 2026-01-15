using System.Threading.Tasks;

namespace MyShop.Contracts
{
    public interface IConfigService
    {
        /// <summary>
        /// Get the configured server address
        /// </summary>
        string GetServerAddress();

        /// <summary>
        /// Set and persist the server address
        /// </summary>
        void SetServerAddress(string address);

        /// <summary>
        /// Test connection to a server address
        /// </summary>
        Task<bool> TestConnectionAsync(string address);

        /// <summary>
        /// Get the default server address
        /// </summary>
        string GetDefaultServerAddress();
    }
}
