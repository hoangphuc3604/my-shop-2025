using MyShop.Services;
using System.Collections.Generic;

namespace MyShop.Contracts
{
    public interface IOnboardingService
    {
        bool IsOnboardingCompleted();
        void MarkOnboardingCompleted();
        void ResetOnboarding();
        List<OnboardingStep> GetOrderPageSteps();
        
        bool IsProductOnboardingCompleted();
        void MarkProductOnboardingCompleted();
        void ResetProductOnboarding();
        List<OnboardingStep> GetProductPageSteps();
    }
}