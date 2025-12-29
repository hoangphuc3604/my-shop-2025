using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyShop.Contracts;
using MyShop.Data;
using MyShop.Data.Models;
using MyShop.Helpers;

namespace MyShop.Services;

public class AccountService : IAccountService
{
    private readonly MyShopDbContext _dbContext;

    public AccountService(MyShopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
        {
            return null;
        }

        if (PasswordHelper.VerifyPassword(password, user.PasswordHash))
        {
            user.LastLogin = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return user;
        }

        return null;
    }
}
