using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace MyShop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private NavigationViewItem? selectedMenuItem;

    [ObservableProperty]
    private object? currentPage;

    public MainViewModel()
    {
    }
}
