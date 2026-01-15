using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services.GraphQL;

namespace MyShop.Services;

public class AuthenticationService : IAccountService
{
    private readonly GraphQLClient _graphQLClient;
    private readonly ISessionService _sessionService;
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public AuthenticationService(GraphQLClient graphQLClient, ISessionService sessionService)
    {
        _graphQLClient = graphQLClient;
        _sessionService = sessionService;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        // Updated to match backend's expected format
        var query = @"
            mutation {
                login(input: {
                    username: """ + username + @"""
                    password: """ + password + @"""
                }) {
                    success
                    token
                    user {
                        userId
                        username
                        email
                        role
                        lastLogin
                    }
                    message
                }
            }
        ";

        try
        {
            Debug.WriteLine("════════════════════════════════════════");
            Debug.WriteLine("[AUTH] LOGIN ATTEMPT");
            Debug.WriteLine("════════════════════════════════════════");
            Debug.WriteLine($"[AUTH] Username: {username}");
            Debug.WriteLine($"[AUTH] Password: {'*' * password.Length}");
            Debug.WriteLine("");
            Debug.WriteLine("[AUTH] GraphQL Query:");
            Debug.WriteLine(query);
            Debug.WriteLine("════════════════════════════════════════");

            var response = await _graphQLClient.QueryAsync<LoginResponse>(query, null, null);

            Debug.WriteLine("");
            Debug.WriteLine("════════════════════════════════════════");
            Debug.WriteLine("[AUTH] LOGIN RESPONSE");
            Debug.WriteLine("════════════════════════════════════════");
            Debug.WriteLine($"[AUTH] Raw response: {JsonSerializer.Serialize(response)}");

            if (response == null)
            {
                Debug.WriteLine("[AUTH] ✗ Response is null");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }

            if (response.Login == null)
            {
                Debug.WriteLine("[AUTH] ✗ Login property is null");
                Debug.WriteLine("════════════════════════════════════════");
                return null;
            }

            Debug.WriteLine($"[AUTH] Success: {response.Login.Success}");
            Debug.WriteLine($"[AUTH] Message: {response.Login.Message}");
            Debug.WriteLine($"[AUTH] Token: {(string.IsNullOrEmpty(response.Login.Token) ? "Not provided" : "***")}");

            if (response.Login.Success && response.Login.User != null)
            {
                var userData = response.Login.User;
                Debug.WriteLine($"[AUTH] ✓ User: {userData.Username}");
                Debug.WriteLine($"[AUTH] UserId: {userData.UserId}");
                Debug.WriteLine($"[AUTH] Email: {userData.Email}");
                Debug.WriteLine($"[AUTH] LastLogin (raw): {userData.LastLogin}");

                // Convert Unix timestamp to DateTime
                var lastLogin = ConvertUnixTimestampToDateTime(userData.LastLogin);
                Debug.WriteLine($"[AUTH] LastLogin (converted): {lastLogin:o}");

                // Save token and role to session
                if (!string.IsNullOrEmpty(response.Login.Token))
                {
                    _sessionService.SaveToken(response.Login.Token);
                    Debug.WriteLine("[AUTH] ✓ Token saved to session");
                }

                if (!string.IsNullOrEmpty(userData.Role))
                {
                    _sessionService.SaveRole(userData.Role);
                    Debug.WriteLine($"[AUTH] ✓ Role saved to session: {userData.Role}");
                }

                var user = new User
                {
                    UserId = userData.UserId,
                    Username = userData.Username ?? string.Empty,
                    Email = userData.Email ?? string.Empty,
                    Role = userData.Role ?? string.Empty,
                    IsActive = userData.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = lastLogin
                };

                Debug.WriteLine("════════════════════════════════════════");
                Debug.WriteLine("[AUTH] ✓ LOGIN SUCCESSFUL");
                return user;
            }

            Debug.WriteLine($"[AUTH] ✗ Login failed: {response.Login.Message}");
            Debug.WriteLine("════════════════════════════════════════");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("");
            Debug.WriteLine("════════════════════════════════════════");
            Debug.WriteLine("[AUTH] ✗ EXCEPTION DURING LOGIN");
            Debug.WriteLine("════════════════════════════════════════");
            Debug.WriteLine($"[AUTH] Exception type: {ex.GetType().Name}");
            Debug.WriteLine($"[AUTH] Exception message: {ex.Message}");
            Debug.WriteLine($"[AUTH] Stack trace: {ex.StackTrace}");
            Debug.WriteLine("════════════════════════════════════════");
            return null;
        }
    }

    /// <summary>
    /// Convert Unix timestamp (in milliseconds) to DateTime
    /// </summary>
    private DateTime ConvertUnixTimestampToDateTime(string? timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
        {
            Debug.WriteLine("[AUTH] LastLogin is null or empty, using current time");
            return DateTime.UtcNow;
        }

        try
        {
            // Try to parse as long (Unix timestamp in milliseconds)
            if (long.TryParse(timestamp, out long unixTimeMs))
            {
                // Convert milliseconds to DateTime
                var dateTime = UnixEpoch.AddMilliseconds(unixTimeMs);
                Debug.WriteLine($"[AUTH] Successfully converted timestamp {unixTimeMs}ms to {dateTime:o}");
                return dateTime;
            }

            // If it's not a number, try parsing as ISO 8601 DateTime
            if (DateTime.TryParse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDateTime))
            {
                Debug.WriteLine($"[AUTH] Successfully parsed timestamp as DateTime: {parsedDateTime:o}");
                return parsedDateTime;
            }

            Debug.WriteLine($"[AUTH] Could not parse timestamp '{timestamp}', using current time");
            return DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AUTH] Exception converting timestamp: {ex.Message}");
            return DateTime.UtcNow;
        }
    }
}