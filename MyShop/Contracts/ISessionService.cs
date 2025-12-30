namespace MyShop.Contracts
{
    public interface ISessionService
    {
        void SaveSession(string username);
        string? GetSavedUsername();
        void ClearSession();
        bool HasValidSession();
    }
}
