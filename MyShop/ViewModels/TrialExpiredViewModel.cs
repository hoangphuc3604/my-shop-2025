using MyShop.Contracts;
using MyShop.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MyShop.ViewModels
{
    public class TrialExpiredViewModel : INotifyPropertyChanged
    {
        private readonly ITrialLicenseService _trialService;
        private string _activationCode = string.Empty;
        private bool _isProcessing;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? ActivationSucceeded;
        public event EventHandler<string>? ActivationFailed;

        public TrialExpiredViewModel(ITrialLicenseService trialService)
        {
            _trialService = trialService ?? throw new ArgumentNullException(nameof(trialService));
        }

        public string ActivationCode
        {
            get => _activationCode;
            set
            {
                if (_activationCode != value)
                {
                    _activationCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    OnPropertyChanged();
                }
            }
        }

        public void ActivateWithCode()
        {
            if (string.IsNullOrWhiteSpace(ActivationCode))
            {
                ActivationFailed?.Invoke(this, "Please enter an activation code");
                return;
            }

            IsProcessing = true;

            try
            {
                if (_trialService.ActivateWithCode(ActivationCode))
                {
                    Debug.WriteLine("[TRIAL_VM] ✓ License activated successfully");
                    ActivationSucceeded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Debug.WriteLine("[TRIAL_VM] ✗ Invalid activation code");
                    ActivationCode = string.Empty;
                    ActivationFailed?.Invoke(this, "✗ Invalid activation code. Please check and try again.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TRIAL_VM] ✗ Activation error: {ex.Message}");
                ActivationFailed?.Invoke(this, $"Activation error: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}