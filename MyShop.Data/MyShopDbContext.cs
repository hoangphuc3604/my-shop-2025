using Microsoft.EntityFrameworkCore;
using MyShop.Data.Models;

namespace MyShop.Data
{
    public class MyShopDbContext : DbContext
    {
        public MyShopDbContext(DbContextOptions<MyShopDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure Category entity
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // Configure Product entity
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl1).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ImageUrl2).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ImageUrl3).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.Sku).IsUnique();
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.CreatedTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasMany(e => e.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId);
                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data
            SeedCategories(modelBuilder);
            SeedProducts(modelBuilder);
        }

        private void SeedCategories(ModelBuilder modelBuilder)
        {
            var categories = new[]
            {
                new Category 
                { 
                    CategoryId = 1, 
                    Name = "Electronics", 
                    Description = "Electronic devices and gadgets for everyday use"
                },
                new Category 
                { 
                    CategoryId = 2, 
                    Name = "Clothing", 
                    Description = "Fashion clothing and apparel for men, women, and children"
                },
                new Category 
                { 
                    CategoryId = 3, 
                    Name = "Home & Garden", 
                    Description = "Home furniture, decor, and garden supplies"
                }
            };

            modelBuilder.Entity<Category>().HasData(categories);
        }

        private void SeedProducts(ModelBuilder modelBuilder)
        {
            var products = new List<Product>();

            // Electronics (CategoryId = 1)
            var electronicsProducts = new[]
            {
                "Wireless Headphones", "USB-C Cable", "Phone Stand", "Portable Charger",
                "Screen Protector", "Phone Case", "Bluetooth Speaker", "USB Hub",
                "HDMI Cable", "Memory Card 64GB", "Webcam", "USB Keyboard",
                "Wireless Mouse", "Laptop Stand", "Monitor Stand", "Cable Organizer",
                "Phone Screen Cleaner", "Charging Dock", "Power Bank 20000mAh", "Type-C Adapter",
                "Lightning Cable", "Network Switch"
            };

            var electronicsImages = new Dictionary<string, string[]>
            {
                { "Wireless Headphones", new[] { "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?q=80&w=2070&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "https://images.unsplash.com/photo-1612465289702-7c84b5258fde?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8V2lyZWxlc3MlMjBIZWFkcGhvbmVzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1619296794093-3df1ae6819a8?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTR8fFdpcmVsZXNzJTIwSGVhZHBob25lc3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "USB-C Cable", new[] { "https://images.unsplash.com/photo-1711056831898-97718f6972d3?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8VVNCLUMlMjBDYWJsZXxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1615086169217-83e1c06c9f4f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fFVTQi1DJTIwQ2FibGV8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1762681290814-432626dffb8c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8VVNCLUMlMjBDYWJsZXxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Phone Stand", new[] { "https://images.unsplash.com/photo-1617975426095-f073792aef15?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8UGhvbmUlMjBTdGFuZHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1553556135-009e5858adce?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8UGhvbmUlMjBTdGFuZHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1707651385176-8c7492596164?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8UGhvbmUlMjBTdGFuZHxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Portable Charger", new[] { "https://images.unsplash.com/photo-1706275399494-fb26bbc5da63?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8UG9ydGFibGUlMjBDaGFyZ2VyfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1736516434209-51ece1006788?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8UG9ydGFibGUlMjBDaGFyZ2VyfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1745888323448-36957ea892f5?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTF8fFBvcnRhYmxlJTIwQ2hhcmdlcnxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Screen Protector", new[] { "https://images.unsplash.com/photo-1714058948949-4414c2007759?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8U2NyZWVuJTIwUHJvdGVjdG9yfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1636614597280-3dde89cbd6cc?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8U2NyZWVuJTIwUHJvdGVjdG9yfGVufDB8fDB8fHwwhttps://images.unsplash.com/photo-1636614597280-3dde89cbd6cc?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8U2NyZWVuJTIwUHJvdGVjdG9yfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1567428486597-8c5328fd3816?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8U2NyZWVuJTIwUHJvdGVjdG9yfGVufDB8fDB8fHww" } },
                { "Phone Case", new[] { "https://images.unsplash.com/photo-1535157412991-2ef801c1748b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8UGhvbmUlMjBDYXNlfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1593055454503-531d165c2ed8?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8UGhvbmUlMjBDYXNlfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1623393884989-cb3663e431c5?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8UGhvbmUlMjBDYXNlfGVufDB8fDB8fHww" } },
                { "Bluetooth Speaker", new[] { "https://images.unsplash.com/photo-1589256469067-ea99122bbdc4?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Ymx1ZXRvb3RoJTIwc3BlYWtlcnxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1589003077984-894e133dabab?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8Ymx1ZXRvb3RoJTIwc3BlYWtlcnxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1588131153911-a4ea5189fe19?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTF8fGJsdWV0b290aCUyMHNwZWFrZXJ8ZW58MHx8MHx8fDA%3D" } },
                { "USB Hub", new[] { "https://images.unsplash.com/photo-1760376789487-994070337c76?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8VVNCJTIwSHVifGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1760376789478-c1023d2dc007?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8VVNCJTIwSHVifGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1616578273461-3a99ce422de6?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8VVNCJTIwSHVifGVufDB8fDB8fHww" } },
                { "HDMI Cable", new[] { "https://images.unsplash.com/photo-1756043827134-dcc8ac7462f6?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8SERNSSUyMENhYmxlfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1760377821967-8bb2f250e041?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8SERNSSUyMENhYmxlfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1649959223405-f927e0fc1e05?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTR8fEhETUklMjBDYWJsZXxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Memory Card 64GB", new[] { "https://images.unsplash.com/photo-1632251350035-7f750a5973b6?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8TWVtb3J5JTIwQ2FyZCUyMDY0R0J8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1739742473106-6d9bb5409d4a?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8TWVtb3J5JTIwQ2FyZCUyMDY0R0J8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1541095740744-65daee21c422?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTh8fE1lbW9yeSUyMENhcmQlMjA2NEdCfGVufDB8fDB8fHww" } },
                { "Webcam", new[] { "https://images.unsplash.com/photo-1623949556303-b0d17d198863?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8d2ViY2FtfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1626581795188-8efb9a00eeec?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fHdlYmNhbXxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1629429407756-446d66f5b24e?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTB8fHdlYmNhbXxlbnwwfHwwfHx8MA%3D%3D" } },
                { "USB Keyboard", new[] { "https://images.unsplash.com/photo-1743862558324-64de6d680fbb?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NTZ8fFVTQiUyMEtleWJvYXJkfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1584727151652-d09b17ebf23f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Njd8fFVTQiUyMEtleWJvYXJkfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1629087667662-632024eabb1e?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NzV8fFVTQiUyMEtleWJvYXJkfGVufDB8fDB8fHww" } },
                { "Wireless Mouse", new[] { "https://images.unsplash.com/photo-1660491083562-d91a64d6ea9c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8V2lyZWxlc3MlMjBNb3VzZXxlbnwwfHwwfHx8MA%3D%3D", "http://images.unsplash.com/photo-1707592691247-5c3a1c7ba0e3?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8V2lyZWxlc3MlMjBNb3VzZXxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1527814050087-3793815479db?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8V2lyZWxlc3MlMjBNb3VzZXxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Laptop Stand", new[] { "https://images.unsplash.com/photo-1623251606108-512c7c4a3507?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8TGFwdG9wJTIwU3RhbmR8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1629317480826-910f729d1709?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8TGFwdG9wJTIwU3RhbmR8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1575399545768-5f1840c1312d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8TGFwdG9wJTIwU3RhbmR8ZW58MHx8MHx8fDA%3D" } },
                { "Monitor Stand", new[] { "https://images.unsplash.com/photo-1760278042013-e6a4866c4c81?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NDZ8fE1vbml0b3IlMjBTdGFuZHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1765805913682-03457777186a?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NDd8fE1vbml0b3IlMjBTdGFuZHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1643131747793-1b103cc66c6b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NDh8fE1vbml0b3IlMjBTdGFuZHxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Cable Organizer", new[] { "https://images.unsplash.com/photo-1760348213270-7cd00b8c3405?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Q2FibGUlMjBPcmdhbml6ZXJ8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1746123725998-c0f6dcdda373?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MjJ8fENhYmxlJTIwT3JnYW5pemVyfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1635335874521-7987db781153?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NTZ8fENhYmxlJTIwT3JnYW5pemVyfGVufDB8fDB8fHww" } },
                { "Phone Screen Cleaner", new[] { "https://images.unsplash.com/photo-1623691136531-68d65f1a25a0?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NTV8fFBob25lJTIwU2NyZWVuJTIwQ2xlYW5lcnxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1657180949872-349249fd1f63?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MjQ4fHxQaG9uZSUyMFNjcmVlbiUyMENsZWFuZXJ8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1603285991346-3f4eb15808d4?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MjZ8fHBob25lJTIwc2NyZWVuJTIwY2xlYW5lcnxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Charging Dock", new[] { "https://images.unsplash.com/photo-1761311985502-8d132eb2ba73?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTZ8fENoYXJnaW5nJTIwRG9ja3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1760462788374-fe0d2d4ba4d1?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTl8fENoYXJnaW5nJTIwRG9ja3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1760462788238-c9111e060692?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MjJ8fENoYXJnaW5nJTIwRG9ja3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Power Bank 20000mAh", new[] { "https://images.unsplash.com/photo-1706275399512-81a6c7267234?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MjR8fHBvd2VyJTIwYmF0dGVyeSUyMDIwMDAwbUFofGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1706275399494-fb26bbc5da63?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NDJ8fHBvd2VyJTIwYmF0dGVyeSUyMDIwMDAwbUFofGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1581104678061-1a4fe238863e?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NzB8fHBvd2VyJTIwYmF0dGVyeSUyMDIwMDAwbUFofGVufDB8fDB8fHww" } },
                { "Type-C Adapter", new[] { "https://images.unsplash.com/photo-1594549181132-9045fed330ce?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8VHlwZS1DJTIwQWRhcHRlcnxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1594549181132-9045fed330ce?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8VHlwZS1DJTIwQWRhcHRlcnxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1594549181132-9045fed330ce?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8VHlwZS1DJTIwQWRhcHRlcnxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Lightning Cable", new[] { "https://images.unsplash.com/photo-1738520420654-87cd2ad005d0?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8TGlnaHRuaW5nJTIwQ2FibGV8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1595392029711-8a206145f976?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8TGlnaHRuaW5nJTIwQ2FibGV8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1607087365600-e7bf50d0b226?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8TGlnaHRuaW5nJTIwQ2FibGV8ZW58MHx8MHx8fDA%3D" } },
                { "Network Switch", new[] { "https://images.unsplash.com/photo-1520869562399-e772f042f422?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8TmV0d29yayUyMFN3aXRjaHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1531765408077-9a1f85f90df1?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8TmV0d29yayUyMFN3aXRjaHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1682559736721-c2e77ff4c650?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8TmV0d29yayUyMFN3aXRjaHxlbnwwfHwwfHx8MA%3D%3D" } }
            };

            int productId = 1;
            for (int i = 0; i < electronicsProducts.Length; i++)
            {
                var productName = electronicsProducts[i];
                var images = electronicsImages[productName];
                var importPrice = 50000 + (i * 15000);
                var count = 100 - (i * 2);

                products.Add(new Product
                {
                    ProductId = productId++,
                    Sku = $"ELEC-{i + 1:D3}",
                    Name = productName,
                    ImportPrice = importPrice,
                    Count = count,
                    Description = $"High-quality {productName} for professional and personal use",
                    ImageUrl1 = images[0],
                    ImageUrl2 = images[1],
                    ImageUrl3 = images[2],
                    CategoryId = 1
                });
            }

            // Clothing (CategoryId = 2)
            var clothingProducts = new[]
            {
                "Cotton T-Shirt", "Jeans", "Polo Shirt", "Casual Shorts",
                "Sweater", "Hoodie", "Jacket", "Chinos Pants",
                "Dress Shirt", "Baseball Cap", "Winter Coat", "Athletic Shoes",
                "Casual Sneakers", "Dress Shoes", "Sandals", "Socks Pack",
                "Leggings", "Tie", "Scarf", "Gloves",
                "Swim Shorts", "Cargo Pants"
            };

            var clothingImages = new Dictionary<string, string[]>
            {
                { "Cotton T-Shirt", new[] { "https://images.unsplash.com/photo-1651761179569-4ba2aa054997?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Y290dG9uJTIwdCUyMHNoaXJ0fGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1759572095384-1a7e646d0d4f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8Y290dG9uJTIwdCUyMHNoaXJ0fGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTh8fGNvdHRvbiUyMHQlMjBzaGlydHxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Jeans", new[] { "https://images.unsplash.com/photo-1602293589930-45aad59ba3ab?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8SmVhbnN8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1542272604-787c3835535d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8SmVhbnN8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1598554747436-c9293d6a588f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8SmVhbnN8ZW58MHx8MHx8fDA%3D" } },
                { "Polo Shirt", new[] { "https://images.unsplash.com/photo-1625910513413-c23b8bb81cba?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8UG9sbyUyMFNoaXJ0fGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1625910513520-bed0389ce32f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8UG9sbyUyMFNoaXJ0fGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1625910513399-c9fcba54338c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8UG9sbyUyMFNoaXJ0fGVufDB8fDB8fHww" } },
                { "Casual Shorts", new[] { "https://images.unsplash.com/photo-1765438867937-90c60e6ce2ec?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MzB8fENhc3VhbCUyMFNob3J0c3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1758541331115-29587869e93f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NDd8fENhc3VhbCUyMFNob3J0c3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1766215565880-d57e8c94538a?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NjB8fENhc3VhbCUyMFNob3J0c3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Sweater", new[] { "https://images.unsplash.com/photo-1574201635302-388dd92a4c3f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8c3dlYXRlcnxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1631541909061-71e349d1f203?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8c3dlYXRlcnxlbnwwfHwwfHx8MA%3D%3D", "https://plus.unsplash.com/premium_photo-1695339146296-ab03c0e6ae50?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NXx8c3dlYXRlcnxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Hoodie", new[] { "https://images.unsplash.com/photo-1620799140188-3b2a02fd9a77?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8SG9vZGllfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1564557287817-3785e38ec1f5?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8SG9vZGllfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1601063476271-a159c71ab0b3?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTB8fEhvb2RpZXxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Jacket", new[] { "https://images.unsplash.com/photo-1521223890158-f9f7c3d5d504?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8SmFja2V0fGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1544022613-e87ca75a784a?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTF8fEphY2tldHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1557418669-db3f781a58c0?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fEphY2tldHxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Chinos Pants", new[] { "https://images.unsplash.com/photo-1584865288642-42078afe6942?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8Q2hpbm9zJTIwUGFudHN8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1764816657611-8b1bd2d72cce?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Q2hpbm9zJTIwUGFudHN8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1610892923471-d8efa48225d0?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MjZ8fENoaW5vcyUyMFBhbnRzfGVufDB8fDB8fHww" } },
                { "Dress Shirt", new[] { "https://images.unsplash.com/photo-1598033129183-c4f50c736f10?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8RHJlc3MlMjBTaGlydHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1603252109612-24fa03d145c8?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8RHJlc3MlMjBTaGlydHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1603252109303-2751441dd157?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8RHJlc3MlMjBTaGlydHxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Baseball Cap", new[] { "https://images.unsplash.com/photo-1720534490358-bc2ad29d51d5?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8QmFzZWJhbGwlMjBDYXB8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1691256676359-20e5c6d4bc92?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8QmFzZWJhbGwlMjBDYXB8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1588850561407-ed78c282e89b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8QmFzZWJhbGwlMjBDYXB8ZW58MHx8MHx8fDA%3D" } },
                { "Winter Coat", new[] { "https://images.unsplash.com/photo-1425100599170-85ec4f00a6ee?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8d2ludGVyJTIwY29hdHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1519944159858-806d435dc86b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8d2ludGVyJTIwY29hdHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1548950939-629ecb4d7101?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8d2ludGVyJTIwY29hdHxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Athletic Shoes", new[] { "https://images.unsplash.com/photo-1765914448113-ebf0ce8cb918?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8QXRobGV0aWMlMjBTaG9lc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1762690285055-fa80848e825b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8QXRobGV0aWMlMjBTaG9lc3xlbnwwfHwwfHx8MA%3D%3D", "http://images.unsplash.com/photo-1761942028306-6e0399c10088?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTF8fEF0aGxldGljJTIwU2hvZXN8ZW58MHx8MHx8fDA%3D" } },
                { "Casual Sneakers", new[] { "https://images.unsplash.com/photo-1612942910539-9ff28b2e00d3?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Q2FzdWFsJTIwU25lYWtlcnN8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1759542890353-35f5568c1c90?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8Q2FzdWFsJTIwU25lYWtlcnN8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1736555142217-916540c7f1b7?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8Q2FzdWFsJTIwU25lYWtlcnN8ZW58MHx8MHx8fDA%3D" } },
                { "Dress Shoes", new[] { "https://images.unsplash.com/photo-1552422554-0d5af0c79fc6?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8RHJlc3MlMjBTaG9lc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1472591651607-70e2d88ae3c4?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8RHJlc3MlMjBTaG9lc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1657034321685-1fba1b2751f3?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8RHJlc3MlMjBTaG9lc3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Sandals", new[] { "https://images.unsplash.com/photo-1603487742131-4160ec999306?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8U2FuZGFsc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1618615098938-84fc29796e76?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8U2FuZGFsc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1562273138-f46be4ebdf33?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8U2FuZGFsc3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Socks Pack", new[] { "https://images.unsplash.com/photo-1694690127800-68314991ee83?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8U29ja3MlMjBQYWNrfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1694690890319-4fc2ec85c302?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTB8fFNvY2tzJTIwUGFja3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1694690890379-12bb810645ff?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fFNvY2tzJTIwUGFja3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Leggings", new[] { "https://images.unsplash.com/photo-1618355281951-a174b87198e2?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8TGVnZ2luZ3N8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1584863495140-a320b13a11a8?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8TGVnZ2luZ3N8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1618259181324-86a49fe68099?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTh8fExlZ2dpbmdzfGVufDB8fDB8fHww" } },
                { "Tie", new[] { "https://images.unsplash.com/photo-1591729652476-e7f587578d9c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8VGllfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1589756823695-278bc923f962?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8VGllfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1591729652581-abd20ff6944a?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTB8fFRpZXxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Scarf", new[] { "https://images.unsplash.com/photo-1693382288218-2ce85aa26974?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8c2NhcmZzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1669053871927-3df53c13194d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8c2NhcmZzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1679934572565-0ceb03f3ec88?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8c2NhcmZzfGVufDB8fDB8fHww" } },
                { "Gloves", new[] { "https://images.unsplash.com/photo-1617118602199-d3c05ae37ed8?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8R2xvdmVzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1611690889004-c009a7e03712?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8R2xvdmVzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1579455134319-042f718340a4?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTV8fEdsb3Zlc3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Swim Shorts", new[] { "https://images.unsplash.com/photo-1631669986792-2bddb5eaf4a0?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8U3dpbSUyMFNob3J0c3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1631669986788-c094d27280dc?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8U3dpbSUyMFNob3J0c3xlbnwwfHwwfHx8MA%3D%3D", "https://plus.unsplash.com/premium_photo-1664392099577-213e2cf77809?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OXx8U3dpbSUyMFNob3J0c3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Cargo Pants", new[] { "https://images.unsplash.com/photo-1584302052177-2e90841dad6a?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Q2FyZ28lMjBQYW50c3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1548883354-7622d03aca27?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8Q2FyZ28lMjBQYW50c3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1649850874075-49e014357b9d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8Q2FyZ28lMjBQYW50c3xlbnwwfHwwfHx8MA%3D%3D" } }
            };

            for (int i = 0; i < clothingProducts.Length; i++)
            {
                var productName = clothingProducts[i];
                var images = clothingImages[productName];
                var importPrice = 100000 + (i * 12000);
                var count = 200 - (i * 3);

                products.Add(new Product
                {
                    ProductId = productId++,
                    Sku = $"CLTH-{i + 1:D3}",
                    Name = productName,
                    ImportPrice = importPrice,
                    Count = count,
                    Description = $"Premium quality {productName} made from comfortable and durable materials",
                    ImageUrl1 = images[0],
                    ImageUrl2 = images[1],
                    ImageUrl3 = images[2],
                    CategoryId = 2
                });
            }

            // Home & Garden (CategoryId = 3)
            var homeGardenProducts = new[]
            {
                "Desk Lamp", "Wall Clock", "Picture Frame", "Yoga Mat",
                "Throw Pillow", "Bed Sheets", "Towel Set", "Kitchen Knife Set",
                "Coffee Maker", "Blender", "Cutting Board", "Storage Boxes",
                "Plant Pot", "Garden Shovel", "Watering Can", "Pruning Shears",
                "Bird Feeder", "Garden Hose", "Outdoor Chair", "Table Lamp",
                "Mirror", "Curtain"
            };

            var homeGardenImages = new Dictionary<string, string[]>
            {
                { "Desk Lamp", new[] { "https://images.unsplash.com/photo-1621447980929-6638614633c8?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8RGVzayUyMExhbXB8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1601642964568-1917224f4e4d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8RGVzayUyMExhbXB8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1526040652367-ac003a0475fe?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8RGVzayUyMExhbXB8ZW58MHx8MHx8fDA%3D" } },
                { "Wall Clock", new[] { "https://images.unsplash.com/photo-1564091880021-bb02f2b2928d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8d2FsbCUyMGNsb2NrfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1563861826100-9cb868fdbe1c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8d2FsbCUyMGNsb2NrfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1602162786736-1575a5b1be76?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8d2FsbCUyMGNsb2NrfGVufDB8fDB8fHww" } },
                { "Picture Frame", new[] { "https://images.unsplash.com/photo-1560828343-a0b3d8864d1b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8UGljdHVyZSUyMEZyYW1lfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1582053628662-c65b0e0544e9?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8UGljdHVyZSUyMEZyYW1lfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1611651525144-42027e7f45ba?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8UGljdHVyZSUyMEZyYW1lfGVufDB8fDB8fHww" } },
                { "Yoga Mat", new[] { "https://images.unsplash.com/photo-1746796751590-a8c0f15d4900?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8ZG9vciUyMG1hdHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1646239646963-b0b9be56d6b5?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8ZG9vciUyMG1hdHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1731325632689-7ecab02d39f3?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NjJ8fGRvb3IlMjBtYXR8ZW58MHx8MHx8fDA%3D" } },
                { "Throw Pillow", new[] { "https://images.unsplash.com/photo-1691256676366-370303d55b61?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8VGhyb3clMjBQaWxsb3d8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1588706235076-627d896e9f67?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8VGhyb3clMjBQaWxsb3d8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1615222599276-3d8149bc51ed?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8VGhyb3clMjBQaWxsb3d8ZW58MHx8MHx8fDA%3D" } },
                { "Bed Sheets", new[] { "https://images.unsplash.com/photo-1542728929-2b5d9a0c8d48?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8QmVkJTIwU2hlZXRzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1601276174812-63280a55656e?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8QmVkJTIwU2hlZXRzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1617744257724-6fafec25442f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8QmVkJTIwU2hlZXRzfGVufDB8fDB8fHww" } },
                { "Towel Set", new[] { "https://images.unsplash.com/photo-1766727923624-2e8eede5aa8c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8VG93ZWwlMjBTZXR8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1739922039544-76de5a8f3077?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTB8fFRvd2VsJTIwU2V0fGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1737065183310-aef762bd011c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fFRvd2VsJTIwU2V0fGVufDB8fDB8fHww" } },
                { "Kitchen Knife Set", new[] { "https://images.unsplash.com/photo-1764177406219-2b4bf3dab69c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8S2l0Y2hlbiUyMEtuaWZlJTIwU2V0fGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1674660346036-4b3df3f07cca?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fEtpdGNoZW4lMjBLbmlmZSUyMFNldHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1609467334293-030ac6448fd8?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTV8fEtpdGNoZW4lMjBLbmlmZSUyMFNldHxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Coffee Maker", new[] { "https://images.unsplash.com/photo-1608354580875-30bd4168b351?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Y29mZmVlJTIwTWFrZXJ8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1707241358597-bafcc8a8e73d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8Y29mZmVlJTIwTWFrZXJ8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1545936055-22b27770efca?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTB8fGNvZmZlZSUyME1ha2VyfGVufDB8fDB8fHww" } },
                { "Blender", new[] { "https://images.unsplash.com/photo-1585237672814-8f85a8118bf6?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8YmxlbmRlcnN8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1570222094114-d054a817e56b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8bWl4ZXVyfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1622818426197-d54f85b88690?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8bWl4ZXVyfGVufDB8fDB8fHww" } },
                { "Cutting Board", new[] { "https://images.unsplash.com/photo-1666013942797-9daa4b8b3b4f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Q3V0dGluZyUyMEJvYXJkfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1587302108992-20648821725d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8Q3V0dGluZyUyMEJvYXJkfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1624811533744-f85d5325d49c?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8Q3V0dGluZyUyMEJvYXJkfGVufDB8fDB8fHww" } },
                { "Storage Boxes", new[] { "https://images.unsplash.com/photo-1609143739217-01b60dad1c67?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8U3RvcmFnZSUyMEJveGVzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1766040923580-16ad32fae8b4?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8U3RvcmFnZSUyMEJveGVzfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1659729751444-dd8e40685a4f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTl8fFN0b3JhZ2UlMjBCb3hlc3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Plant Pot", new[] { "https://images.unsplash.com/photo-1609061801093-1b7a8a574c69?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8UGxhbnQlMjBQb3R8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1588440691140-09155c1be58a?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8UGxhbnQlMjBQb3R8ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1563419837758-e48ef1b731dd?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8UGxhbnQlMjBQb3R8ZW58MHx8MHx8fDA%3D" } },
                { "Garden Shovel", new[] { "https://images.unsplash.com/photo-1562090098-1929cdffc7c2?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8R2FyZGVuJTIwU2hvdmVsfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1597868233489-5be16be259ed?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8R2FyZGVuJTIwU2hvdmVsfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1712912828055-3d01804a1c73?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8R2FyZGVuJTIwU2hvdmVsfGVufDB8fDB8fHww" } },
                { "Watering Can", new[] { "https://images.unsplash.com/photo-1599277100479-3252d492a19a?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8V2F0ZXJpbmclMjBDYW58ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1515150144380-bca9f1650ed9?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8V2F0ZXJpbmclMjBDYW58ZW58MHx8MHx8fDA%3D", "https://images.unsplash.com/photo-1661880667340-625347ecf85b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8V2F0ZXJpbmclMjBDYW58ZW58MHx8MHx8fDA%3D" } },
                { "Pruning Shears", new[] { "https://images.unsplash.com/photo-1588311082740-88c1b480d72d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8UHJ1bmluZyUyMFNoZWFyc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1680124744737-03fb697f303d?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8UHJ1bmluZyUyMFNoZWFyc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1680124744736-859f16257ef0?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8UHJ1bmluZyUyMFNoZWFyc3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Bird Feeder", new[] { "https://images.unsplash.com/photo-1598280860721-14d1fd80a8af?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8QmlyZCUyMEZlZWRlcnxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1590238750866-f99d3febe496?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8QmlyZCUyMEZlZWRlcnxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1621807147195-580cddd8f758?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8QmlyZCUyMEZlZWRlcnxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Garden Hose", new[] { "https://images.unsplash.com/photo-1693776472225-be367ccf88b7?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Z2FyZGVuJTIwaG9zZXxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1586876757274-7ba27e45cd6b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8Z2FyZGVuJTIwaG9zZXxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1684867430779-e66e779a19b7?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8Z2FyZGVuJTIwaG9zZXxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Outdoor Chair", new[] { "https://images.unsplash.com/photo-1622789095519-c6a1accef0a6?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8b3V0ZG9vciUyMGNoYWlyc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1767014732583-7cf8e781f72e?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8b3V0ZG9vciUyMGNoYWlyc3xlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1764090550152-23b8dbf2908f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8b3V0ZG9vciUyMGNoYWlyc3xlbnwwfHwwfHx8MA%3D%3D" } },
                { "Table Lamp", new[] { "https://images.unsplash.com/photo-1642689703534-e41f29622078?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8VGFibGUlMjBMYW1wfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1580130281320-0ef0754f2bf7?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8VGFibGUlMjBMYW1wfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1573676386604-78f8ed228e2b?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8VGFibGUlMjBMYW1wfGVufDB8fDB8fHww" } },
                { "Mirror", new[] { "https://images.unsplash.com/photo-1519577918463-484ce33e1ecb?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8TWlycm9yfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1656797590428-653803a957fd?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8TWlycm9yfGVufDB8fDB8fHww", "https://images.unsplash.com/photo-1620416264626-84e3c7dbe91f?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTB8fE1pcnJvcnxlbnwwfHwwfHx8MA%3D%3D" } },
                { "Curtain", new[] { "https://images.unsplash.com/photo-1734498230542-b36f63c17f85?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Q3VydGFpbiUyMFJvZHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1593909840438-48825492a8b7?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8NHx8Q3VydGFpbiUyMFJvZHxlbnwwfHwwfHx8MA%3D%3D", "https://images.unsplash.com/photo-1539208175673-6b9149754096?w=500&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8Q3VydGFpbiUyMFJvZHxlbnwwfHwwfHx8MA%3D%3D" } }
            };

            for (int i = 0; i < homeGardenProducts.Length; i++)
            {
                var productName = homeGardenProducts[i];
                var images = homeGardenImages[productName];
                var importPrice = 75000 + (i * 20000);
                var count = 150 - (i * 2);

                products.Add(new Product
                {
                    ProductId = productId++,
                    Sku = $"HOME-{i + 1:D3}",
                    Name = productName,
                    ImportPrice = importPrice,
                    Count = count,
                    Description = $"Quality {productName} for your home and garden needs",
                    ImageUrl1 = images[0],
                    ImageUrl2 = images[1],
                    ImageUrl3 = images[2],
                    CategoryId = 3
                });
            }

            modelBuilder.Entity<Product>().HasData(products);
        }
    }
}