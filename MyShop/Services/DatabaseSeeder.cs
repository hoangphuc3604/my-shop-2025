using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Data;
using MyShop.Helpers;

namespace MyShop.Services;

public static class DatabaseSeeder
{
    public static async Task SeedDemoDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyShopDbContext>();

        try
        {
            await db.Database.MigrateAsync();
        }
        catch
        {
            // ignore migration errors for demo scenarios
        }

        if (!await db.Users.AnyAsync(u => u.Username == "admin"))
        {
            var user = new MyShop.Data.Models.User
            {
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = PasswordHelper.HashPassword("password"),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
    }
}


