namespace MyShop.Tests.Contracts
{
    public interface IAuthorizationService
    {
        string? GetRole();
        bool HasPermission(string permission);
    }
}