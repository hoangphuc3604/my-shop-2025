using System.Collections.Generic;
using MyShop.Contracts;

namespace MyShop.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ISessionService _sessionService;
        private static readonly Dictionary<string, List<string>> RolePermissions = new()
        {
            ["ADMIN"] = new List<string>
            {
                "READ_PRODUCTS",
                "CREATE_PRODUCTS",
                "UPDATE_PRODUCTS",
                "DELETE_PRODUCTS",
                "READ_CATEGORIES",
                "CREATE_CATEGORIES",
                "UPDATE_CATEGORIES",
                "DELETE_CATEGORIES",
                "READ_ORDERS",
                "CREATE_ORDERS",
                "UPDATE_ORDERS",
                "DELETE_ORDERS",
                "VIEW_DASHBOARD",
                "VIEW_REPORTS"
            },
            ["SALE"] = new List<string>
            {
                "READ_PRODUCTS",
                "READ_CATEGORIES",
                "CREATE_ORDERS",
                "READ_ORDERS"
            }
        };

        public AuthorizationService(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public string? GetRole()
        {
            return _sessionService.GetRole();
        }

        public bool HasPermission(string permission)
        {
            var role = _sessionService.GetRole();
            if (string.IsNullOrEmpty(role))
            {
                return false;
            }

            return RolePermissions.TryGetValue(role, out var permissions) &&
                   permissions.Contains(permission);
        }
    }
}
