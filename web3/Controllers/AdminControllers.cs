// Controllers/AdminController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ecom.Data;
using ecom.Models;
using ecom.Services;
using System.Security.Claims;

namespace ecom.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAliExpressApiService _aliExpressService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IAliExpressApiService aliExpressService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _aliExpressService = aliExpressService;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet("import")]
        public IActionResult ImportProduct()
        {
            return View();
        }

        [HttpGet("products")]
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        [HttpGet("customers")]
        public async Task<IActionResult> Customers()
        {
            var customers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(customers);
        }

        [HttpGet("settings")]
        public IActionResult Settings()
        {
            return View();
        }

        // === API Endpoints ===

        [HttpGet("api/stats")]
        public async Task<IActionResult> GetStats()
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var stats = new
            {
                monthlyRevenue = await _context.Orders
                    .Where(o => o.CreatedAt >= startOfMonth)
                    .SumAsync(o => o.TotalAmount),

                monthlyOrders = await _context.Orders
                    .CountAsync(o => o.CreatedAt >= startOfMonth),

                totalProducts = await _context.Products
                    .CountAsync(p => p.IsActive),

                totalCustomers = await _context.Users.CountAsync()
            };

            return Ok(stats);
        }

        [HttpGet("api/recent-orders")]
        public async Task<IActionResult> GetRecentOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    customerEmail = o.User.Email,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("api/low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var products = await _context.Products
                .Where(p => p.IsActive && p.StockQuantity <= 10)
                .OrderBy(p => p.StockQuantity)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.StockQuantity
                })
                .ToListAsync();

            return Ok(products);
        }

        // AliExpress API endpoints
        [HttpPost("api/aliexpress/fetch")]
        public async Task<IActionResult> FetchAliExpressProduct([FromBody] FetchRequest request)
        {
            try
            {
                // Utiliser le service AliExpress pour récupérer les données
                var product = await _aliExpressService.ImportProductFromUrl(request.Url);

                if (product == null)
                {
                    return BadRequest(new { success = false, message = "Impossible de récupérer le produit" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        title = product.Name,
                        description = product.Description,
                        price = product.Price,
                        currency = "USD",
                        sku = product.SKU,
                        images = new[] { product.ImageUrl }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching AliExpress product");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("api/aliexpress/import")]
        public async Task<IActionResult> ImportAliExpressProduct([FromBody] ImportRequest request)
        {
            try
            {
                // Créer le produit
                var product = new Product
                {
                    Name = request.OriginalData.Title,
                    Description = request.OriginalData.Description +
                                 (string.IsNullOrEmpty(request.AdditionalDescription) ?
                                  "" : "\n\n" + request.AdditionalDescription),
                    Price = request.SellingPrice,
                    StockQuantity = request.StockQuantity,
                    SKU = request.OriginalData.Sku ?? GenerateSku(),
                    ImageUrl = request.OriginalData.Images?.FirstOrDefault(),
                    CategoryId = request.CategoryId,
                    Brand = "AliExpress",
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Produit importé avec succès",
                    productId = product.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing AliExpress product");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private string GenerateSku()
        {
            return $"AE-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        public class FetchRequest
        {
            public string Url { get; set; }
        }

        public class ImportRequest
        {
            public dynamic OriginalData { get; set; }
            public int CategoryId { get; set; }
            public int? SubCategoryId { get; set; }
            public decimal Margin { get; set; }
            public int StockQuantity { get; set; }
            public decimal SellingPrice { get; set; }
            public string AdditionalDescription { get; set; }
            public bool IsActive { get; set; }
        }
    }
}