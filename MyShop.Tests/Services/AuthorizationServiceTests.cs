using Xunit;
using Moq;

namespace MyShop.Tests.Services
{
    public interface IMockSessionService
    {
        string? GetRole();
    }

    public class AuthorizationServiceForTesting
    {
        private readonly IMockSessionService _sessionService;
        private static readonly Dictionary<string, List<string>> RolePermissions = new()
        {
            ["ADMIN"] = new List<string>
            {
                "READ_PRODUCTS", "CREATE_PRODUCTS", "UPDATE_PRODUCTS", "DELETE_PRODUCTS",
                "READ_CATEGORIES", "CREATE_CATEGORIES", "UPDATE_CATEGORIES", "DELETE_CATEGORIES",
                "READ_ORDERS", "CREATE_ORDERS", "UPDATE_ORDERS", "DELETE_ORDERS",
                "VIEW_DASHBOARD", "VIEW_REPORTS",
                "READ_PROMOTIONS", "CREATE_PROMOTIONS", "UPDATE_PROMOTIONS", "DELETE_PROMOTIONS"
            },
            ["SALE"] = new List<string>
            {
                "READ_PRODUCTS", "READ_CATEGORIES", "CREATE_ORDERS", "READ_ORDERS",
                "UPDATE_ORDERS", "READ_PROMOTIONS"
            }
        };

        public AuthorizationServiceForTesting(IMockSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public string? GetRole() => _sessionService.GetRole();

        public bool HasPermission(string permission)
        {
            var role = _sessionService.GetRole();
            if (string.IsNullOrEmpty(role))
                return false;

            return RolePermissions.TryGetValue(role, out var permissions) &&
                   permissions.Contains(permission);
        }
    }

    public class AuthorizationServiceTests
    {
        private readonly AuthorizationServiceForTesting _authorizationService;
        private readonly Mock<IMockSessionService> _mockSessionService;

        public AuthorizationServiceTests()
        {
            _mockSessionService = new Mock<IMockSessionService>();
            _authorizationService = new AuthorizationServiceForTesting(_mockSessionService.Object);
        }

        #region GetRole Tests

        [Fact]
        public void GetRole_WhenSessionHasRole_ReturnsRole()
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns("ADMIN");

            // Act
            var result = _authorizationService.GetRole();

            // Assert
            Assert.Equal("ADMIN", result);
        }

        [Fact]
        public void GetRole_WhenSessionHasNoRole_ReturnsNull()
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns((string)null);

            // Act
            var result = _authorizationService.GetRole();

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region HasPermission Tests - ADMIN

        [Theory]
        [InlineData("READ_PRODUCTS")]
        [InlineData("CREATE_PRODUCTS")]
        [InlineData("DELETE_ORDERS")]
        [InlineData("VIEW_DASHBOARD")]
        public void HasPermission_AdminWithValidPermissions_ReturnsTrue(string permission)
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns("ADMIN");

            // Act
            var result = _authorizationService.HasPermission(permission);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasPermission_AdminWithInvalidPermission_ReturnsFalse()
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns("ADMIN");

            // Act
            var result = _authorizationService.HasPermission("INVALID_PERMISSION");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region HasPermission Tests - SALE

        [Theory]
        [InlineData("READ_PRODUCTS")]
        [InlineData("CREATE_ORDERS")]
        [InlineData("READ_PROMOTIONS")]
        public void HasPermission_SaleWithValidPermissions_ReturnsTrue(string permission)
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns("SALE");

            // Act
            var result = _authorizationService.HasPermission(permission);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("CREATE_PRODUCTS")]
        [InlineData("VIEW_DASHBOARD")]
        [InlineData("DELETE_ORDERS")]
        public void HasPermission_SaleWithRestrictedPermissions_ReturnsFalse(string permission)
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns("SALE");

            // Act
            var result = _authorizationService.HasPermission(permission);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region HasPermission Tests - No Role

        [Fact]
        public void HasPermission_NoRoleWithAnyPermission_ReturnsFalse()
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns((string)null);

            // Act
            var result = _authorizationService.HasPermission("READ_PRODUCTS");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasPermission_EmptyRoleWithAnyPermission_ReturnsFalse()
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns(string.Empty);

            // Act
            var result = _authorizationService.HasPermission("READ_PRODUCTS");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region HasPermission Tests - Unknown Role

        [Fact]
        public void HasPermission_UnknownRoleWithAnyPermission_ReturnsFalse()
        {
            // Arrange
            _mockSessionService.Setup(s => s.GetRole()).Returns("UNKNOWN_ROLE");

            // Act
            var result = _authorizationService.HasPermission("READ_PRODUCTS");

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}