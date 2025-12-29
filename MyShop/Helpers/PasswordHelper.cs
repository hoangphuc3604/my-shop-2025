using System;
using System.Text;
using BCrypt.Net;

namespace MyShop.Helpers;

public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));
        if (string.IsNullOrWhiteSpace(hashedPassword)) return false;
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}


