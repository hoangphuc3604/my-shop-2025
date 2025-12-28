using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyShop.Data
{
    public class MyShopDbContextFactory : IDesignTimeDbContextFactory<MyShopDbContext>
    {
        public MyShopDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MyShopDbContext>();
            
            // Use the connection string from environment or default
            var connectionString = Environment.GetEnvironmentVariable("MYSHOP_CONNECTION_STRING") 
                ?? "Host=localhost;Port=5432;Database=myshop_db;Username=postgres;Password=password";
            
            optionsBuilder.UseNpgsql(connectionString);
            
            return new MyShopDbContext(optionsBuilder.Options);
        }
    }
}