using MyShop.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace MyShop.Services
{
    public class OnboardingService
    {
        private const string OnboardingCompletedKey = "OnboardingCompleted";
        private const string ProductOnboardingCompletedKey = "ProductOnboardingCompleted";
        private readonly ISessionService _sessionService;

        public OnboardingService(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public bool IsOnboardingCompleted()
        {
            var completed = Windows.Storage.ApplicationData.Current.LocalSettings.Values[OnboardingCompletedKey];
            return completed != null && (bool)completed;
        }

        public void MarkOnboardingCompleted()
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[OnboardingCompletedKey] = true;
        }

        public void ResetOnboarding()
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove(OnboardingCompletedKey);
        }

        public bool IsProductOnboardingCompleted()
        {
            var completed = Windows.Storage.ApplicationData.Current.LocalSettings.Values[ProductOnboardingCompletedKey];
            return completed != null && (bool)completed;
        }

        public void MarkProductOnboardingCompleted()
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[ProductOnboardingCompletedKey] = true;
        }

        public void ResetProductOnboarding()
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove(ProductOnboardingCompletedKey);
        }

        public List<OnboardingStep> GetOrderPageSteps()
        {
            return new List<OnboardingStep>
            {
                new OnboardingStep
                {
                    Title = "Welcome to Orders! 👋",
                    Description = "This is where you manage all your orders. Let's take a quick tour!",
                    TargetName = "AddOrderButton"
                },
                new OnboardingStep
                {
                    Title = "Create New Orders",
                    Description = "Click here to create a new order for your customers.",
                    TargetName = "AddOrderButton"
                },
                new OnboardingStep
                {
                    Title = "Filter by Date Range",
                    Description = "Use these date pickers to filter orders within a specific time period.",
                    TargetName = "FromDatePicker"
                },
                new OnboardingStep
                {
                    Title = "Sort and Organize",
                    Description = "Sort orders by creation time or price, and customize how many items you see per page.",
                    TargetName = "SortCriteriaCombo"
                },
                new OnboardingStep
                {
                    Title = "View Order Details",
                    Description = "Click any order row to view, edit, or delete orders. You're all set!",
                    TargetName = "OrdersDataGrid"
                }
            };
        }

        public List<OnboardingStep> GetProductPageSteps()
        {
            return new List<OnboardingStep>
            {
                new OnboardingStep
                {
                    Title = "Welcome to Products! 🛍️",
                    Description = "Manage your inventory here. Let's explore the key features!",
                    TargetName = "StatusTextBlock"
                },
                new OnboardingStep
                {
                    Title = "Quick Actions",
                    Description = "Add new products, categories, import bulk data, or refresh the list with these buttons.",
                    TargetName = "AddProductButton"
                },
                new OnboardingStep
                {
                    Title = "Filter Your Products",
                    Description = "Filter products by category, price range, or search by name to find exactly what you need.",
                    TargetName = "CategoryFilter"
                },
                new OnboardingStep
                {
                    Title = "Sort and Paginate",
                    Description = "Sort products and adjust items per page to customize your view.",
                    TargetName = "ItemsPerPageCombo"
                },
                new OnboardingStep
                {
                    Title = "Product Actions",
                    Description = "View, edit, or delete products directly from the list. You're ready to go!",
                    TargetName = "ProductsListView"
                }
            };
        }
    }

    public class OnboardingStep
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
    }
}