using System;
using Windows.Storage;
using MyShop.Contracts;

namespace MyShop.Services
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        private const string UsernameKey = "SavedUsername";
        private const string SessionTimestampKey = "SessionTimestamp";
        private const int SessionExpiryHours = 7 * 24; // 7 days

        public void SaveSession(string username)
        {
            _localSettings.Values[UsernameKey] = username;
            _localSettings.Values[SessionTimestampKey] = DateTime.Now.ToString("o");
        }

        public string? GetSavedUsername()
        {
            return _localSettings.Values[UsernameKey] as string;
        }

        public void ClearSession()
        {
            _localSettings.Values.Remove(UsernameKey);
            _localSettings.Values.Remove(SessionTimestampKey);
        }

        public bool HasValidSession()
        {
            var username = GetSavedUsername();
            if (string.IsNullOrEmpty(username))
                return false;

            var timestampString = _localSettings.Values[SessionTimestampKey] as string;
            if (string.IsNullOrEmpty(timestampString))
                return false;

            if (DateTime.TryParse(timestampString, out var timestamp))
            {
                var expiryTime = timestamp.AddHours(SessionExpiryHours);
                return DateTime.Now < expiryTime;
            }

            return false;
        }
    }
}
