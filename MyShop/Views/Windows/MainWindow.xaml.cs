using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShop.ViewModels;

namespace MyShop.Views.Windows;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
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
            _ => null
        };

        if (pageType is not null)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
