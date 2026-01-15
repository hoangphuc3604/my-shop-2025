using System;
using System.Diagnostics;
using Windows.Storage;
using MyShop.Contracts;

namespace MyShop.Services
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        private const string UsernameKey = "SavedUsername";
        private const string SessionTimestampKey = "SessionTimestamp";
        private const string AuthTokenKey = "AuthToken";
        private const string TokenTimestampKey = "TokenTimestamp";
        private const string LastPageKey = "LastPage";
        private const string RoleKey = "UserRole";
        private const int SessionExpiryHours = 7 * 24; // 7 days

        public void SaveSession(string username, string? token = null)
        {
            _localSettings.Values[UsernameKey] = username;
            _localSettings.Values[SessionTimestampKey] = DateTime.Now.ToString("o");
            
            if (!string.IsNullOrEmpty(token))
            {
                SaveToken(token);
            }

            Debug.WriteLine($"[SESSION] Session saved for user: {username}");
        }

        public string? GetSavedUsername()
        {
            return _localSettings.Values[UsernameKey] as string;
        }

        public void ClearSession()
        {
            _localSettings.Values.Remove(UsernameKey);
            _localSettings.Values.Remove(SessionTimestampKey);
            _localSettings.Values.Remove(AuthTokenKey);
            _localSettings.Values.Remove(TokenTimestampKey);
            _localSettings.Values.Remove(LastPageKey);

            Debug.WriteLine("[SESSION] Session cleared");
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

        public void SaveToken(string token)
        {
            _localSettings.Values[AuthTokenKey] = token;
            _localSettings.Values[TokenTimestampKey] = DateTime.UtcNow.ToString("o");

            Debug.WriteLine("[SESSION] ✓ Authentication token saved");
        }

        public string? GetAuthToken()
        {
            var token = _localSettings.Values[AuthTokenKey] as string;
            
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("[SESSION] ✗ No authentication token found");
                return null;
            }

            Debug.WriteLine("[SESSION] ✓ Authentication token retrieved");
            return token;
        }

        public bool HasValidToken()
        {
            var token = _localSettings.Values[AuthTokenKey] as string;
            return !string.IsNullOrEmpty(token);
        }

        public void SaveLastPage(string pageName)
        {
            _localSettings.Values[LastPageKey] = pageName;
            Debug.WriteLine($"[SESSION] Last page saved: {pageName}");
        }

        public string? GetLastPage()
        {
            var lastPage = _localSettings.Values[LastPageKey] as string;
            Debug.WriteLine($"[SESSION] Retrieved last page: {lastPage ?? "Dashboard (default)"}");
            return lastPage;
        }

        public void SaveRole(string role)
        {
            _localSettings.Values[RoleKey] = role;
            Debug.WriteLine($"[SESSION] User role saved: {role}");
        }

        public string? GetRole()
        {
            var role = _localSettings.Values[RoleKey] as string;
            Debug.WriteLine($"[SESSION] Retrieved user role: {role ?? "Not set"}");
            return role;
        }
    }
}
