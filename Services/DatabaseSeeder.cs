using Microsoft.EntityFrameworkCore;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Models.Enums;

namespace webprogbackend.Services
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync();
    }

    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Seed admin user
                await SeedAdminUserAsync();

                // Seed sample products if none exist
                await SeedSampleProductsAsync();

                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private async Task SeedAdminUserAsync()
        {
            var adminSettings = _configuration.GetSection("AdminSettings");
            var adminEmail = adminSettings["DefaultAdminEmail"];
            var adminUsername = adminSettings["DefaultAdminUserName"];
            var adminPassword = adminSettings["DefaultAdminPassword"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
            {
                _logger.LogWarning("Admin settings not configured properly. Skipping admin user creation.");
                return;
            }

            // Check if admin user already exists
            var existingAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail || u.Role == UserRole.Admin);

            if (existingAdmin != null)
            {
                _logger.LogInformation("Admin user already exists. Skipping admin user creation.");
                return;
            }

            var adminUser = new User
            {
                Username = adminUsername,
                Email = adminEmail,
                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Admin user created successfully with email: {adminEmail}");
        }

        private async Task SeedSampleProductsAsync()
        {
            if (await _context.Products.AnyAsync())
            {
                _logger.LogInformation("Products already exist. Skipping sample product creation.");
                return;
            }

            var sampleProducts = new List<Product>
            {
                new Product
                {
                    Name = "Laptop Bilgisayar",
                    Description = "Yüksek performanslı dizüstü bilgisayar. Intel i7 işlemci, 16GB RAM, 512GB SSD.",
                    Price = 15999.99m,
                    StockQuantity = 25,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/0066CC/FFFFFF?text=Laptop",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Akıllı Telefon",
                    Description = "Son nesil akıllı telefon. 128GB depolama, 48MP kamera, 5G destekli.",
                    Price = 8999.99m,
                    StockQuantity = 50,
                    Category = "Telefon",
                    ImageUrl = "https://via.placeholder.com/300x300/009900/FFFFFF?text=Phone",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Bluetooth Kulaklık",
                    Description = "Kablosuz Bluetooth kulaklık. Aktif gürültü engelleme, 30 saat pil ömrü.",
                    Price = 599.99m,
                    StockQuantity = 100,
                    Category = "Ses",
                    ImageUrl = "https://via.placeholder.com/300x300/CC6600/FFFFFF?text=Headphones",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Gaming Mouse",
                    Description = "Profesyonel oyuncu faresi. RGB aydınlatma, 16000 DPI, programlanabilir tuşlar.",
                    Price = 299.99m,
                    StockQuantity = 75,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/CC0066/FFFFFF?text=Mouse",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "4K Monitör",
                    Description = "27 inç 4K UHD monitör. IPS panel, HDR10 desteği, USB-C bağlantısı.",
                    Price = 3299.99m,
                    StockQuantity = 20,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/6600CC/FFFFFF?text=Monitor",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Mekanik Klavye",
                    Description = "RGB aydınlatmalı mekanik klavye. Cherry MX anahtarlar, programlanabilir makrolar.",
                    Price = 899.99m,
                    StockQuantity = 60,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/009999/FFFFFF?text=Keyboard",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Tablet",
                    Description = "10.9 inç tablet. 256GB depolama, Apple Pencil desteği, 10 saat pil ömrü.",
                    Price = 4999.99m,
                    StockQuantity = 30,
                    Category = "Tablet",
                    ImageUrl = "https://via.placeholder.com/300x300/CC3300/FFFFFF?text=Tablet",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Akıllı Saat",
                    Description = "Spor ve sağlık takibi özellikli akıllı saat. GPS, kalp ritmi monitörü, su geçirmez.",
                    Price = 1999.99m,
                    StockQuantity = 40,
                    Category = "Giyilebilir",
                    ImageUrl = "https://via.placeholder.com/300x300/996600/FFFFFF?text=Watch",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Taşınabilir SSD",
                    Description = "1TB taşınabilir SSD. USB 3.2 Gen 2, şifreleme desteği, dayanıklı yapı.",
                    Price = 1299.99m,
                    StockQuantity = 80,
                    Category = "Depolama",
                    ImageUrl = "https://via.placeholder.com/300x300/666600/FFFFFF?text=SSD",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Webcam",
                    Description = "4K webcam. Otomatik odaklama, stereo mikrofon, geniş görüş açısı.",
                    Price = 799.99m,
                    StockQuantity = 35,
                    Category = "Ses",
                    ImageUrl = "https://via.placeholder.com/300x300/336699/FFFFFF?text=Webcam",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Products.AddRange(sampleProducts);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created {sampleProducts.Count} sample products");
        }
    }
}