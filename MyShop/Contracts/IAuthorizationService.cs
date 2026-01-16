namespace MyShop.Contracts
{
    public interface IAuthorizationService
    {
        string? GetRole();
        bool HasPermission(string permission);
    }
}


