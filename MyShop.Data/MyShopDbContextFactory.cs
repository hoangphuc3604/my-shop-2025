using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;
using System.IO;

namespace MyShop.Data
{
    public class MyShopDbContextFactory : IDesignTimeDbContextFactory<MyShopDbContext>
    {
        public MyShopDbContext CreateDbContext(string[] args)
        {
            // Load .env file from the project root
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
            }

            var optionsBuilder = new DbContextOptionsBuilder<MyShopDbContext>();

            // Read from .env variables - required, no fallback
            var dbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "myshop_db";
            var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
            var dbPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

            if (string.IsNullOrEmpty(dbPassword))
            {
                throw new InvalidOperationException("POSTGRES_PASSWORD environment variable is not set in .env file");
            }

            var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

            optionsBuilder.UseNpgsql(connectionString);

            return new MyShopDbContext(optionsBuilder.Options);
        }
    }
}