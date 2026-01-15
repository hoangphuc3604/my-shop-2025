using System;
using MyShop.Contracts;

namespace MyShop.Services
{
    public class NavigationService : INavigationService
    {
        public event EventHandler<NavigationEventArgs>? NavigationRequested;

        public void NavigateToMain()
        {
            NavigateTo(NavigationTarget.Main);
        }

        public void NavigateToLogin()
        {
            NavigateTo(NavigationTarget.Login);
        }

        public void NavigateTo(NavigationTarget target)
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs { Target = target });
        }
    }
}
