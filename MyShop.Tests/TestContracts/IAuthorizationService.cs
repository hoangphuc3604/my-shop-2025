namespace MyShop.Tests.TestContracts
{
    public interface IAuthorizationService
    {
        string? GetRole();
        bool HasPermission(string permission);
    }
}