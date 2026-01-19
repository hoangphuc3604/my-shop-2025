using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShop.Contracts;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace MyShop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _appVersion;

    [ObservableProperty]
    private bool _isRememberMe;

    public LoginViewModel(IAccountService accountService, INavigationService navigationService, ISessionService sessionService)
    {
        _accountService = accountService;
        _navigationService = navigationService;
        _sessionService = sessionService;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersion = $"Version: {version}";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        Debug.WriteLine("LoginAsync started");
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Debug.WriteLine("Validation failed: empty username or password");
            ErrorMessage = "Please enter both username and password.";
            return;
        }

        Debug.WriteLine($"Attempting login for '{Username}'");
        try
        {
            var user = await _accountService.LoginAsync(Username, Password);
            Debug.WriteLine(user is null ? "Login service returned null" : $"Login service returned user {user.Username}");
            if (user != null)
            {
                if (IsRememberMe)
                {
                    _sessionService.SaveSession(Username);
                }
                _sessionService.SaveRole(user.Role);
                _navigationService.NavigateToMain();
            }
            else
            {
                Debug.WriteLine("Setting ErrorMessage: Invalid username or password.");
                ErrorMessage = "Invalid username or password.";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Login exception: {ex}");
            ErrorMessage = "An error occurred while attempting to sign in. Please try again.";
        }
    }

    public void ClearSession()
    {
        _sessionService.ClearSession();
    }
}
