namespace MyShop.Contracts
{
    public interface ISessionService
    {
        // Session management
        void SaveSession(string username, string? token = null);
        string? GetSavedUsername();
        void ClearSession();
        bool HasValidSession();

        // Token management
        void SaveToken(string token);
        string? GetAuthToken();
        bool HasValidToken();
    }
}
