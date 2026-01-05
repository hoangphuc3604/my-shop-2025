using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.ViewModels;
using Windows.Graphics;

namespace MyShop.Views.Windows;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;

        // Set window size
        this.Title = "MyShop";
        this.AppWindow.ResizeClient(new SizeInt32(1440, 750));

        // Set default page to Dashboard
        Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object? sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        if (ViewModel.SelectedMenuItem == null)
        {
            ViewModel.NavigateToDashboard(DashboardItem);
        }
        Activated -= MainWindow_Activated; // Remove event handler after first activation
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

            ViewModel.SelectedMenuItem = item;
            NavigateToPage(item.Content?.ToString());
        }
    }

    private void NavigateToPage(string? pageName)
    {
        Type? pageType = pageName switch
        {
            "Dashboard" => typeof(Views.Pages.DashboardPage),
            "Products" => typeof(Views.Pages.ProductPage),
            "Orders" => typeof(Views.Pages.OrderPage),
            "Report" => typeof(Views.Pages.ReportPage),
            _ => null
        };

        if (pageType is not null)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
