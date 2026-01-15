using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using MyShop.Contracts;

namespace MyShop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IAuthorizationService _authorizationService;

    [ObservableProperty]
    private NavigationViewItem? selectedMenuItem;

    [ObservableProperty]
    private object? currentPage;

    [ObservableProperty]
    private bool _canManageProducts;

    [ObservableProperty]
    private bool _canViewCategories;

    [ObservableProperty]
    private bool _canViewOrders;

    [ObservableProperty]
    private bool _canViewDashboard;

    [ObservableProperty]
    private bool _canViewReports;

    public MainViewModel(ISessionService sessionService, INavigationService navigationService, IAuthorizationService authorizationService)
    {
        _sessionService = sessionService;
        _navigationService = navigationService;
        _authorizationService = authorizationService;

        InitializePermissions();
    }

    private void InitializePermissions()
    {
        CanManageProducts = _authorizationService.HasPermission("CREATE_PRODUCTS");
        CanViewCategories = _authorizationService.HasPermission("READ_CATEGORIES");
        CanViewOrders = _authorizationService.HasPermission("READ_ORDERS");
        CanViewDashboard = _authorizationService.HasPermission("VIEW_DASHBOARD");
        CanViewReports = _authorizationService.HasPermission("VIEW_REPORTS");
    }

    public void NavigateToDashboard(NavigationViewItem dashboardItem)
    {
        SelectedMenuItem = dashboardItem;
    }

    [RelayCommand]
    private void Logout()
    {
        _sessionService.ClearSession();
        _navigationService.NavigateToLogin();
    }
}
