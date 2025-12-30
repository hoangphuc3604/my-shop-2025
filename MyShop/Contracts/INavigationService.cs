using System;

namespace MyShop.Contracts
{
    public interface INavigationService
    {
        event EventHandler<NavigationEventArgs>? NavigationRequested;
        void NavigateToMain();
        void NavigateToLogin();
    }

    public class NavigationEventArgs : EventArgs
    {
        public NavigationTarget Target { get; set; }
    }

    public enum NavigationTarget
    {
        Login,
        Main
    }
}
