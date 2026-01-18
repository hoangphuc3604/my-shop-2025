namespace MyShop.Tests.TestContracts
{
    public interface ISessionService
    {
        string? GetRole();
        string? GetAuthToken();
    }
}