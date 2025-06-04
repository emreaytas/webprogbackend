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

                // Seed test users with different roles
                await SeedTestUsersAsync();

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

        private async Task SeedTestUsersAsync()
        {
            // Test kullanıcıları oluştur (sadece development ortamında)
            if (!_configuration.GetValue<bool>("SeedTestData", false))
            {
                return;
            }

            var testUsers = new[]
            {
                new { Username = "testuser", Email = "user@test.com", Password = "Test123!", Role = UserRole.User },
                new { Username = "testadmin2", Email = "admin2@test.com", Password = "Test123!", Role = UserRole.Admin }
            };

            foreach (var testUser in testUsers)
            {
                if (!await _context.Users.AnyAsync(u => u.Email == testUser.Email))
                {
                    var user = new User
                    {
                        Username = testUser.Username,
                        Email = testUser.Email,
                        Password = BCrypt.Net.BCrypt.HashPassword(testUser.Password),
                        Role = testUser.Role,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);

                    // Normal kullanıcılar için cart oluştur
                    if (testUser.Role == UserRole.User)
                    {
                        var cart = new Cart
                        {
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Carts.Add(cart);
                    }

                    _logger.LogInformation($"Test user created: {testUser.Email} with role: {testUser.Role}");
                }
            }

            await _context.SaveChangesAsync();
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
                    Name = "MacBook Pro 14",
                    Description = "Apple MacBook Pro 14 inç, M3 Pro çip, 18GB RAM, 512GB SSD. Profesyonel iş yükü için mükemmel performans.",
                    Price = 45999.99m,
                    StockQuantity = 15,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/1f1f1f/FFFFFF?text=MacBook+Pro",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Dell XPS 13 Plus",
                    Description = "Intel Core i7-1360P, 32GB RAM, 1TB SSD, 13.4 inç OLED dokunmatik ekran.",
                    Price = 35999.99m,
                    StockQuantity = 22,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/0066CC/FFFFFF?text=Dell+XPS",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "iPhone 15 Pro Max",
                    Description = "Apple iPhone 15 Pro Max, 256GB, Titanium Blue. A17 Pro çip, Pro kamera sistemi.",
                    Price = 52999.99m,
                    StockQuantity = 35,
                    Category = "Telefon",
                    ImageUrl = "https://via.placeholder.com/300x300/1f1f1f/FFFFFF?text=iPhone+15",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Samsung Galaxy S24 Ultra",
                    Description = "Samsung Galaxy S24 Ultra, 512GB, Titanium Black. Snapdragon 8 Gen 3, S Pen dahil.",
                    Price = 48999.99m,
                    StockQuantity = 28,
                    Category = "Telefon",
                    ImageUrl = "https://via.placeholder.com/300x300/1428A0/FFFFFF?text=Galaxy+S24",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Sony WH-1000XM5",
                    Description = "Sony WH-1000XM5 Kablosuz Noise Cancelling Kulaklık. 30 saat pil ömrü, premium ses kalitesi.",
                    Price = 12999.99m,
                    StockQuantity = 45,
                    Category = "Ses",
                    ImageUrl = "https://via.placeholder.com/300x300/000000/FFFFFF?text=Sony+WH1000XM5",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "AirPods Pro 2. Nesil",
                    Description = "Apple AirPods Pro (2. nesil) MagSafe şarj kutusu ile. Adaptif Transparency, Kişiselleştirilmiş Spatial Audio.",
                    Price = 8999.99m,
                    StockQuantity = 67,
                    Category = "Ses",
                    ImageUrl = "https://via.placeholder.com/300x300/FFFFFF/000000?text=AirPods+Pro",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "iPad Pro 12.9 M2",
                    Description = "Apple iPad Pro 12.9 inç, M2 çip, 256GB WiFi + Cellular. Liquid Retina XDR ekran.",
                    Price = 38999.99m,
                    StockQuantity = 18,
                    Category = "Tablet",
                    ImageUrl = "https://via.placeholder.com/300x300/E5E5E7/000000?text=iPad+Pro",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Samsung Galaxy Tab S9 Ultra",
                    Description = "Samsung Galaxy Tab S9 Ultra, 512GB, 14.6 inç Dynamic AMOLED 2X ekran, S Pen dahil.",
                    Price = 34999.99m,
                    StockQuantity = 12,
                    Category = "Tablet",
                    ImageUrl = "https://via.placeholder.com/300x300/1428A0/FFFFFF?text=Tab+S9+Ultra",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Apple Watch Series 9",
                    Description = "Apple Watch Series 9 GPS + Cellular, 45mm, Midnight Aluminum Case. watchOS 10, Siri sorguları.",
                    Price = 16999.99m,
                    StockQuantity = 33,
                    Category = "Giyilebilir",
                    ImageUrl = "https://via.placeholder.com/300x300/000000/FFFFFF?text=Apple+Watch",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Samsung Galaxy Watch6 Classic",
                    Description = "Samsung Galaxy Watch6 Classic, 47mm, Stainless Steel. Gelişmiş sağlık takibi, döner çerçeve.",
                    Price = 12999.99m,
                    StockQuantity = 26,
                    Category = "Giyilebilir",
                    ImageUrl = "https://via.placeholder.com/300x300/1428A0/FFFFFF?text=Galaxy+Watch6",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Samsung T7 Portable SSD 2TB",
                    Description = "Samsung T7 Portable SSD 2TB. USB 3.2 Gen 2, 1050MB/s okuma hızı, AES 256-bit şifreleme.",
                    Price = 4999.99m,
                    StockQuantity = 55,
                    Category = "Depolama",
                    ImageUrl = "https://via.placeholder.com/300x300/1428A0/FFFFFF?text=T7+SSD",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "SanDisk Extreme Pro Portable SSD 4TB",
                    Description = "SanDisk Extreme Pro Portable SSD 4TB. USB-C 3.2 Gen 2x2, 2000MB/s aktarım hızı.",
                    Price = 12999.99m,
                    StockQuantity = 31,
                    Category = "Depolama",
                    ImageUrl = "https://via.placeholder.com/300x300/FF6600/FFFFFF?text=SanDisk+Extreme",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Logitech MX Master 3S",
                    Description = "Logitech MX Master 3S Wireless Mouse. Sessiz tıklama, MagSpeed elektromanyetik kaydırma.",
                    Price = 3299.99m,
                    StockQuantity = 78,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/017EFA/FFFFFF?text=MX+Master+3S",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Keychron K8 Pro Mechanical Keyboard",
                    Description = "Keychron K8 Pro Kablosuz Mekanik Klavye. Gateron Pro anahtarlar, RGB backlighting.",
                    Price = 4599.99m,
                    StockQuantity = 42,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/2D2D2D/FFFFFF?text=Keychron+K8",
                    CreatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "LG UltraGear 27GP950-B",
                    Description = "LG UltraGear 27 inç 4K UHD Nano IPS Gaming Monitör. 144Hz, HDR600, USB-C 90W.",
                    Price = 23999.99m,
                    StockQuantity = 14,
                    Category = "Bilgisayar",
                    ImageUrl = "https://via.placeholder.com/300x300/A40000/FFFFFF?text=LG+UltraGear",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Products.AddRange(sampleProducts);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created {sampleProducts.Count} sample products with various categories and price ranges");
        }
    }
}