using System;
using System.Diagnostics;
using Windows.Storage;
using MyShop.Contracts;
using System.Collections.Generic;

namespace MyShop.Services
{
    public interface ITrialLicenseService
    {
        /// <summary>
        /// Get trial status
        /// </summary>
        TrialStatus GetTrialStatus();

        /// <summary>
        /// Get remaining trial days
        /// </summary>
        int GetRemainingTrialDays();

        /// <summary>
        /// Activate license with activation code
        /// </summary>
        bool ActivateWithCode(string activationCode);

        /// <summary>
        /// Check if app is activated (not in trial mode)
        /// </summary>
        bool IsActivated();

        /// <summary>
        /// Reset trial for testing purposes
        /// </summary>
        void ResetTrial();
        void SetTrialExpirationToNow();
    }

    public enum TrialStatus
    {
        Active,           // Trial is still active
        Expired,          // Trial has expired
        Activated,        // App is activated with license
        FirstRun          // First run - start trial
    }

    public class TrialLicenseService : ITrialLicenseService
    {
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private const string TrialStartDateKey = "TrialStartDate";
        private const string ActivationCodeKey = "ActivationCode";
        private const int TRIAL_DAYS = 15;

        // Simple activation codes (in production, validate with server)
        private static readonly List<string> ValidActivationCodes = new List<string>
        {
            "MYSHOP-2025-TRIAL",
            "MYSHOP-PREMIUM-01",
            "MYSHOP-BUSINESS-01"
        };

        public TrialStatus GetTrialStatus()
        {
            // Check if already activated
            var activationCode = _localSettings.Values[ActivationCodeKey] as string;
            if (!string.IsNullOrEmpty(activationCode))
            {
                Debug.WriteLine("[TRIAL] ✓ App is activated");
                return TrialStatus.Activated;
            }

            // Check trial start date
            var trialStartStr = _localSettings.Values[TrialStartDateKey] as string;
            if (string.IsNullOrEmpty(trialStartStr))
            {
                Debug.WriteLine("[TRIAL] First run - starting 15-day trial");
                _localSettings.Values[TrialStartDateKey] = DateTime.Now.ToString("o");
                return TrialStatus.FirstRun;
            }

            // Check if trial has expired
            if (DateTime.TryParse(trialStartStr, out var trialStartDate))
            {
                var trialEndDate = trialStartDate.AddDays(TRIAL_DAYS);
                if (DateTime.Now > trialEndDate)
                {
                    Debug.WriteLine("[TRIAL] ✗ Trial has expired");
                    return TrialStatus.Expired;
                }

                Debug.WriteLine("[TRIAL] ✓ Trial is still active");
                return TrialStatus.Active;
            }

            return TrialStatus.FirstRun;
        }

        public int GetRemainingTrialDays()
        {
            var trialStartStr = _localSettings.Values[TrialStartDateKey] as string;
            if (string.IsNullOrEmpty(trialStartStr))
                return TRIAL_DAYS;

            if (DateTime.TryParse(trialStartStr, out var trialStartDate))
            {
                var trialEndDate = trialStartDate.AddDays(TRIAL_DAYS);
                var remainingDays = (trialEndDate - DateTime.Now).Days;
                return Math.Max(0, remainingDays);
            }

            return TRIAL_DAYS;
        }

        public bool ActivateWithCode(string activationCode)
        {
            // Validate activation code
            if (string.IsNullOrWhiteSpace(activationCode))
            {
                Debug.WriteLine("[TRIAL] ✗ Activation code is empty");
                return false;
            }

            // Trim and uppercase
            activationCode = activationCode.Trim().ToUpper();

            // Check if code is valid
            if (!ValidActivationCodes.Contains(activationCode))
            {
                Debug.WriteLine($"[TRIAL] ✗ Invalid activation code: {activationCode}");
                return false;
            }

            // Save activation code
            _localSettings.Values[ActivationCodeKey] = activationCode;
            _localSettings.Values[TrialStartDateKey] = DateTime.Now.ToString("o");

            Debug.WriteLine($"[TRIAL] ✓ App activated with code: {activationCode}");
            return true;
        }

        public bool IsActivated()
        {
            var activationCode = _localSettings.Values[ActivationCodeKey] as string;
            return !string.IsNullOrEmpty(activationCode);
        }

        public void ResetTrial()
        {
            _localSettings.Values.Remove(TrialStartDateKey);
            _localSettings.Values.Remove(ActivationCodeKey);
            Debug.WriteLine("[TRIAL] Trial reset for testing");
        }

        // 🆕 ADD THIS METHOD for easy testing
        public void SetTrialExpirationToNow()
        {
            var startedDateToBeExpired = DateTime.Now.AddDays(-16); // Set to yesterday
            _localSettings.Values[TrialStartDateKey] = startedDateToBeExpired.ToString("o");
            _localSettings.Values.Remove(ActivationCodeKey);
            Debug.WriteLine("[TRIAL] Trial set to expired for testing");
        }
    }
}