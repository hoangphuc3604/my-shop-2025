using System;

namespace MyShop.Contracts
{
    public interface INavigationService
    {
        event EventHandler<NavigationEventArgs>? NavigationRequested;
        void NavigateToMain();
        void NavigateToLogin();
        void NavigateTo(NavigationTarget target);
    }

    public class NavigationEventArgs : EventArgs
    {
        public NavigationTarget Target { get; set; }
    }

    public enum NavigationTarget
    {
        Login,
        Main,
        Config
    }
}
