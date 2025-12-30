using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using MyShop.Contracts;

namespace MyShop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private NavigationViewItem? selectedMenuItem;

    [ObservableProperty]
    private object? currentPage;

    public MainViewModel(ISessionService sessionService, INavigationService navigationService)
    {
        _sessionService = sessionService;
        _navigationService = navigationService;
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
