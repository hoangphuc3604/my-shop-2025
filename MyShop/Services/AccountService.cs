using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyShop.Contracts;
using MyShop.Data;
using MyShop.Data.Models;

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

        // TODO: Implement password hashing comparison
        if (user.PasswordHash == password)
        {
            user.LastLogin = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return user;
        }

        return null;
    }
}
