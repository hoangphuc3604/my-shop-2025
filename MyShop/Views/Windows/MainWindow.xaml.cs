using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.ViewModels;
using MyShop.Contracts;
using Windows.Graphics;
using System.Diagnostics;

namespace MyShop.Views.Windows;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly IAuthorizationService _authorizationService;

    public MainWindow(MainViewModel viewModel, IAuthorizationService authorizationService)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _authorizationService = authorizationService;

        // Set window size
        this.Title = "MyShop";
        this.AppWindow.ResizeClient(new SizeInt32(1440, 750));

        // Set default page to Dashboard or last visited page
        Activated += MainWindow_Activated;
        UpdateMenuVisibility();
    }

    private void MainWindow_Activated(object? sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        if (ViewModel.SelectedMenuItem == null)
        {
            // Get last visited page from session
            var sessionService = App.Services.GetService(typeof(ISessionService)) as ISessionService;
            var lastPage = sessionService?.GetLastPage();

            NavigationViewItem? targetItem = lastPage switch
            {
                "Products" => ProductsItem,
                "Orders" => OrdersItem,
                "Promotions" => PromotionItem,
                "Report" when _authorizationService.HasPermission("VIEW_REPORTS") => ReportItem,
                _ => ViewModel.CanViewDashboard ? DashboardItem : ProductsItem
            };

            Debug.WriteLine($"[MAINWINDOW] Restoring to page: {lastPage ?? "Dashboard"}");
            ViewModel.NavigateToDashboard(targetItem ?? (ViewModel.CanViewDashboard ? DashboardItem : ProductsItem));
        }
        Activated -= MainWindow_Activated;
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            if (item == LogoutItem)
            {
                ViewModel.LogoutCommand.Execute(null);
                return;
            }

            if (item == DashboardItem && !ViewModel.CanViewDashboard)
            {
                return;
            }

            if (item == PromotionItem && !_authorizationService.HasPermission("READ_PROMOTIONS"))
            {
                return;
            }

            if (item == ReportItem && !ViewModel.CanViewReports)
            {
                return;
            }

            ViewModel.SelectedMenuItem = item;
            NavigateToPage(item.Content?.ToString());
        }
    }

    private void NavigateToPage(string? pageName)
    {
        string? requiredPermission = pageName switch
        {
            "Dashboard" => "VIEW_DASHBOARD",
            "Report" => "VIEW_REPORTS",
            _ => null
        };

        if (requiredPermission != null && !_authorizationService.HasPermission(requiredPermission))
        {
            return;
        }

        Type? pageType = pageName switch
        {
            "Dashboard" => typeof(Views.Pages.DashboardPage),
            "Products" => typeof(Views.Pages.ProductPage),
            "Orders" => typeof(Views.Pages.OrderPage),
            "Promotions" => typeof(Views.Pages.PromotionPage),
            "Report" => typeof(Views.Pages.ReportPage),
            _ => null
        };

        if (pageType is not null)
        {
            // Save the current page to session
            var sessionService = App.Services.GetService(typeof(ISessionService)) as ISessionService;
            sessionService?.SaveLastPage(pageName ?? "Dashboard");

            ContentFrame.Navigate(pageType);
        }
    }

    private void UpdateMenuVisibility()
    {
        DashboardItem.Visibility = ViewModel.CanViewDashboard ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        ReportItem.Visibility = ViewModel.CanViewReports ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }
}
