using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Security.Claims;
using webprogbackend.Data;
using webprogbackend.Models;

namespace webprogbackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext context, IConfiguration configuration, ILogger<OrdersController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Sipari� bulunamad�" });

            if (order.UserId != userId)
                return Forbid();

            return order;
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<object>> CreateOrder([FromBody] CreateOrderModel model)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                _logger.LogInformation($"Sipari� olu�turma ba�lad� - UserId: {userId}");
                _logger.LogInformation($"Shipping Address: {model.ShippingAddress}");

                // Kullan�c�n�n sepetini kontrol et
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                // Sepet yoksa veya bo�sa alternatif olarak Urun tablosuna bak
                if (cart == null || !cart.CartItems.Any())
                {
                    _logger.LogWarning($"Cart sepeti bo�, Urun tablosuna bak�l�yor - UserId: {userId}");

                    // Urun tablosundan sepet ��elerini al
                    var urunItems = await _context.Uruns
                        .Where(u => u.UserId == userId)
                        .ToListAsync();

                    if (!urunItems.Any())
                    {
                        _logger.LogWarning($"Hem Cart hem Urun tablosu bo� - UserId: {userId}");
                        return BadRequest(new
                        {
                            message = "Sepetiniz bo�. L�tfen �nce �r�n ekleyin.",
                            details = "Ne Cart ne de Urun tablosunda �r�n bulunamad�"
                        });
                    }

                    // Urun tablosundaki �r�nlerden sipari� olu�tur
                    var productIds = urunItems.Select(u => u.UrunId).ToList();
                    var products = await _context.Products
                        .Where(p => productIds.Contains(p.Id))
                        .ToListAsync();

                    if (!products.Any())
                    {
                        return BadRequest(new
                        {
                            message = "Sepetinizdeki �r�nler art�k mevcut de�il",
                            details = "Urun tablosundaki �r�nler Products tablosunda bulunamad�"
                        });
                    }

                    // Toplam tutar� hesapla
                    var totalAmount = products.Sum(p => p.Price);

                    // Sipari� olu�tur
                    var order = new Order
                    {
                        UserId = userId,
                        OrderNumber = GenerateOrderNumber(),
                        TotalAmount = totalAmount,
                        Status = "Pending",
                        ShippingAddress = model.ShippingAddress,
                        PaymentIntentId = null, // Stripe entegrasyonu olmadan
                        OrderItems = products.Select(p => new OrderItem
                        {
                            ProductId = p.Id,
                            Quantity = 1, // Urun tablosunda miktar yok, 1 kabul et
                            UnitPrice = p.Price
                        }).ToList()
                    };

                    _context.Orders.Add(order);

                    // Stok g�ncelleme
                    foreach (var product in products)
                    {
                        if (product.StockQuantity > 0)
                        {
                            product.StockQuantity -= 1;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    // Urun tablosunu temizle
                    _context.Uruns.RemoveRange(urunItems);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Sipari� ba�ar�yla olu�turuldu (Urun tablosundan) - OrderId: {order.Id}, OrderNumber: {order.OrderNumber}");

                    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new
                    {
                        success = true,
                        orderId = order.Id,
                        orderNumber = order.OrderNumber,
                        totalAmount = order.TotalAmount,
                        message = "Sipari� ba�ar�yla olu�turuldu",
                        source = "UrunTable"
                    });
                }

                // Normal Cart tablosundan sipari� olu�turma
                _logger.LogInformation($"Cart sepeti bulundu - {cart.CartItems.Count} �r�n");

                // Stok kontrol�
                foreach (var item in cart.CartItems)
                {
                    if (item.Product.StockQuantity < item.Quantity)
                    {
                        return BadRequest(new
                        {
                            message = $"'{item.Product.Name}' �r�n� i�in yeterli stok yok. Mevcut: {item.Product.StockQuantity}, �stenen: {item.Quantity}"
                        });
                    }
                }

                // Stripe Payment Intent olu�tur (iste�e ba�l�)
                string paymentIntentId = null;
                try
                {
                    var paymentIntentService = new PaymentIntentService();
                    var paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
                    {
                        Amount = (long)(cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity) * 100), // Convert to cents
                        Currency = "try",
                        PaymentMethodTypes = new List<string> { "card" }
                    });
                    paymentIntentId = paymentIntent.Id;
                }
                catch (Exception stripeEx)
                {
                    _logger.LogWarning(stripeEx, "Stripe Payment Intent olu�turulamad�, sipari� Stripe olmadan devam ediyor");
                }

                var normalOrder = new Order
                {
                    UserId = userId,
                    OrderNumber = GenerateOrderNumber(),
                    TotalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity),
                    Status = "Pending",
                    ShippingAddress = model.ShippingAddress,
                    PaymentIntentId = paymentIntentId,
                    OrderItems = cart.CartItems.Select(ci => new OrderItem
                    {
                        ProductId = ci.ProductId,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.Product.Price
                    }).ToList()
                };

                _context.Orders.Add(normalOrder);

                // Update product stock
                foreach (var item in cart.CartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Clear cart
                cart.CartItems.Clear();
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sipari� ba�ar�yla olu�turuldu (Cart tablosundan) - OrderId: {normalOrder.Id}, OrderNumber: {normalOrder.OrderNumber}");

                return CreatedAtAction(nameof(GetOrder), new { id = normalOrder.Id }, new
                {
                    success = true,
                    orderId = normalOrder.Id,
                    orderNumber = normalOrder.OrderNumber,
                    totalAmount = normalOrder.TotalAmount,
                    clientSecret = paymentIntentId != null ? "payment_intent_created" : null,
                    message = "Sipari� ba�ar�yla olu�turuldu",
                    source = "CartTable"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipari� olu�turma s�ras�nda hata olu�tu");
                return StatusCode(500, new
                {
                    message = "Sipari� olu�turulurken bir hata olu�tu",
                    details = ex.Message
                });
            }
        }

        // PUT: api/Orders/5/Status
        [HttpPut("{id}/Status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusModel model)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { message = "Sipari� bulunamad�" });

                order.Status = model.Status;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sipari� durumu g�ncellendi - OrderId: {id}, NewStatus: {model.Status}");

                return Ok(new { message = "Sipari� durumu g�ncellendi", status = model.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sipari� durumu g�ncellenirken hata olu�tu - OrderId: {id}");
                return StatusCode(500, new { message = "Sipari� durumu g�ncellenirken hata olu�tu" });
            }
        }

        // Sipari� numaras� olu�tur
        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        // GET: api/Orders/Recent - Son sipari�ler
        [HttpGet("Recent")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecentOrders([FromQuery] int count = 5)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var recentOrders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt,
                    ItemCount = o.OrderItems.Count()
                })
                .ToListAsync();

            return Ok(recentOrders);
        }

        // GET: api/Orders/Statistics - Sipari� istatistikleri
        [HttpGet("Statistics")]
        public async Task<ActionResult<object>> GetOrderStatistics()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var stats = await _context.Orders
                .Where(o => o.UserId == userId)
                .GroupBy(o => 1)
                .Select(g => new
                {
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    PendingOrders = g.Count(o => o.Status == "Pending"),
                    CompletedOrders = g.Count(o => o.Status == "Delivered"),
                    CancelledOrders = g.Count(o => o.Status == "Cancelled")
                })
                .FirstOrDefaultAsync();

            return Ok(stats ?? new
            {
                TotalOrders = 0,
                TotalSpent = 0m,
                PendingOrders = 0,
                CompletedOrders = 0,
                CancelledOrders = 0
            });
        }
    }

    public class CreateOrderModel
    {
        public string ShippingAddress { get; set; }
    }

    public class UpdateOrderStatusModel
    {
        public string Status { get; set; }
    }
}