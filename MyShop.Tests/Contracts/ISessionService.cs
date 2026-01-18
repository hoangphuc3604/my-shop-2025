namespace MyShop.Tests.Contracts
{
    public interface ISessionService
    {
        string? GetRole();
        string? GetAuthToken();
    }
}