using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webprogbackend.Data;
using webprogbackend.Models;
using webprogbackend.Models.Enums;
using webprogbackend.Attributes;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] string searchTerm = null,
            [FromQuery] string category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? inStock = null,
            [FromQuery] string sortBy = "name",
            [FromQuery] string sortOrder = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Products.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category.ToLower() == category.ToLower());
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (inStock.HasValue)
            {
                if (inStock.Value)
                    query = query.Where(p => p.StockQuantity > 0);
                else
                    query = query.Where(p => p.StockQuantity == 0);
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "price" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "stock" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
                "created" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Add pagination metadata to response headers
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Total-Pages", totalPages.ToString());
            Response.Headers.Add("X-Current-Page", page.ToString());

            return products;
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // GET: api/Products/TopSelling
        [HttpGet("TopSelling")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopSellingProducts([FromQuery] int count = 3)
        {
            var topProducts = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(count)
                .Join(_context.Products,
                      tp => tp.ProductId,
                      p => p.Id,
                      (tp, p) => new
                      {
                          Product = p,
                          TotalQuantitySold = tp.TotalQuantitySold,
                          TotalRevenue = tp.TotalRevenue
                      })
                .ToListAsync();

            return topProducts;
        }

        // GET: api/Products/Featured
        [HttpGet("Featured")]
        public async Task<ActionResult<IEnumerable<Product>>> GetFeaturedProducts([FromQuery] int count = 6)
        {
            // En yeni ürünler veya stok durumu iyi olan ürünler
            var featuredProducts = await _context.Products
                .Where(p => p.StockQuantity > 0)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return featuredProducts;
        }

        // GET: api/Products/LowStock
        [HttpGet("LowStock")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<IEnumerable<Product>>> GetLowStockProducts([FromQuery] int threshold = 10)
        {
            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= threshold && p.StockQuantity > 0)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            return lowStockProducts;
        }

        // GET: api/Products/OutOfStock
        [HttpGet("OutOfStock")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<IEnumerable<Product>>> GetOutOfStockProducts()
        {
            var outOfStockProducts = await _context.Products
                .Where(p => p.StockQuantity == 0)
                .ToListAsync();

            return outOfStockProducts;
        }

        // POST: api/Products
        [HttpPost]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<Product>> CreateProduct(CreateProductModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                Category = model.Category,
                ImageUrl = model.ImageUrl ?? ""
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // POST: api/Products/Bulk
        [HttpPost("Bulk")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<IEnumerable<Product>>> CreateBulkProducts(List<CreateProductModel> models)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var products = models.Select(model => new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                Category = model.Category,
                ImageUrl = model.ImageUrl ?? ""
            }).ToList();

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            return Ok(products);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductModel model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            product.Name = model.Name;
            product.Description = model.Description;
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.Category = model.Category;
            product.ImageUrl = model.ImageUrl;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Products/5/Stock
        [HttpPatch("{id}/Stock")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateModel model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.StockQuantity = model.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Products/5/Price
        [HttpPatch("{id}/Price")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> UpdatePrice(int id, [FromBody] PriceUpdateModel model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Price = model.Price;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product is in any orders or carts
            var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            var hasCartItems = await _context.CartItems.AnyAsync(ci => ci.ProductId == id);

            if (hasOrders || hasCartItems)
            {
                return BadRequest("Cannot delete product that has been ordered or is in carts");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/Stats
        [HttpGet("Stats")]
        [AuthorizeRoles(UserRole.Admin)]
        public async Task<ActionResult<object>> GetProductStats()
        {
            var totalProducts = await _context.Products.CountAsync();
            var totalInStock = await _context.Products.CountAsync(p => p.StockQuantity > 0);
            var totalOutOfStock = await _context.Products.CountAsync(p => p.StockQuantity == 0);
            var totalLowStock = await _context.Products.CountAsync(p => p.StockQuantity <= 10 && p.StockQuantity > 0);

            var stats = new
            {
                TotalProducts = totalProducts,
                TotalInStock = totalInStock,
                TotalOutOfStock = totalOutOfStock,
                TotalLowStock = totalLowStock,
                AveragePrice = totalProducts > 0 ? await _context.Products.AverageAsync(p => p.Price) : 0,
                TotalInventoryValue = await _context.Products.SumAsync(p => p.Price * p.StockQuantity),
                Categories = await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        TotalValue = g.Sum(p => p.Price * p.StockQuantity)
                    })
                    .ToListAsync()
            };

            return stats;
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }

    // DTO Models
    public class CreateProductModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateProductModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
    }

    public class StockUpdateModel
    {
        public int StockQuantity { get; set; }
    }

    public class PriceUpdateModel
    {
        public decimal Price { get; set; }
    }
}