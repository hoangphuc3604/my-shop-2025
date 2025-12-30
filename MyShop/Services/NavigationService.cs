using System;
using MyShop.Contracts;

namespace MyShop.Services
{
    public class NavigationService : INavigationService
    {
        public event EventHandler<NavigationEventArgs>? NavigationRequested;

        public void NavigateToMain()
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs { Target = NavigationTarget.Main });
        }

        public void NavigateToLogin()
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs { Target = NavigationTarget.Login });
        }
    }
}
