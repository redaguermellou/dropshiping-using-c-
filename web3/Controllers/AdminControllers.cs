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

        // ========== PRODUCT CRUD OPERATIONS ==========

        // GET: admin/products
        [HttpGet("products")]
        public async Task<IActionResult> Products(string search = "", string sortBy = "newest", int categoryId = 0)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.SKU.Contains(search) ||
                    p.Description.Contains(search));
            }

            // Category filter
            if (categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // Sorting
            switch (sortBy.ToLower())
            {
                case "name":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "price_asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "stock":
                    query = query.OrderBy(p => p.StockQuantity);
                    break;
                case "oldest":
                    query = query.OrderBy(p => p.CreatedAt);
                    break;
                default: // newest
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            var products = await query.ToListAsync();
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(products);
        }

        // GET: admin/products/create
        [HttpGet("products/create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // POST: admin/products/create
        [HttpPost("products/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    product.CreatedAt = DateTime.UtcNow;
                    product.UpdatedAt = DateTime.UtcNow;

                    // Generate SKU if empty
                    if (string.IsNullOrEmpty(product.SKU))
                    {
                        product.SKU = $"PROD-{DateTime.UtcNow:yyyyMMddHHmmss}";
                    }

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Produit créé avec succès !";
                    return RedirectToAction(nameof(Products));
                }

                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la création du produit.";
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View(product);
            }
        }

        // GET: admin/products/edit/{id}
        [HttpGet("products/edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Produit non trouvé.";
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        // POST: admin/products/edit/{id}
        [HttpPost("products/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                TempData["ErrorMessage"] = "ID du produit invalide.";
                return RedirectToAction(nameof(Products));
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = await _context.Products.FindAsync(id);
                    if (existingProduct == null)
                    {
                        TempData["ErrorMessage"] = "Produit non trouvé.";
                        return RedirectToAction(nameof(Products));
                    }

                    // Update fields
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.SKU = product.SKU;
                    existingProduct.ImageUrl = product.ImageUrl;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.Brand = product.Brand;
                    existingProduct.IsActive = product.IsActive;
                    existingProduct.UpdatedAt = DateTime.UtcNow;

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Produit mis à jour avec succès !";
                    return RedirectToAction(nameof(Products));
                }

                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product {id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la mise à jour du produit.";
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View(product);
            }
        }

        // GET: admin/products/details/{id}
        [HttpGet("products/details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Produit non trouvé.";
                return RedirectToAction(nameof(Products));
            }

            return View(product);
        }

        // POST: admin/products/delete/{id}
        [HttpPost("products/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Produit non trouvé.";
                    return RedirectToAction(nameof(Products));
                }

                // Soft delete or hard delete based on your preference
                // Option 1: Soft delete (recommended)
                // product.IsActive = false;
                // product.UpdatedAt = DateTime.UtcNow;

                // Option 2: Hard delete
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Produit supprimé avec succès !";
                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product {id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la suppression du produit. Il y a peut-être des commandes associées.";
                return RedirectToAction(nameof(Products));
            }
        }

        // GET: admin/products/toggle-status/{id}
        [HttpGet("products/toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Produit non trouvé.";
                    return RedirectToAction(nameof(Products));
                }

                product.IsActive = !product.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Produit {(product.IsActive ? "activé" : "désactivé")} avec succès !";
                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling status for product {id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors du changement de statut.";
                return RedirectToAction(nameof(Products));
            }
        }

        // AJAX: admin/products/update-stock/{id}
        [HttpPost("products/update-stock/{id}")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] int quantity)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Produit non trouvé." });
                }

                product.StockQuantity = quantity;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Stock mis à jour.", newStock = quantity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stock for product {id}");
                return Json(new { success = false, message = "Erreur lors de la mise à jour du stock." });
            }
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

        [HttpGet("analytics")]
        public IActionResult Analytics()
        {
            return View();
        }

        [HttpGet("api/analytics")]
        public async Task<IActionResult> GetAnalytics(string period = "7days")
        {
            var now = DateTime.UtcNow;
            DateTime startDate;
            DateTime previousStartDate;

            switch (period)
            {
                case "today":
                    startDate = now.Date;
                    previousStartDate = startDate.AddDays(-1);
                    break;
                case "30days":
                    startDate = now.AddDays(-30);
                    previousStartDate = startDate.AddDays(-30);
                    break;
                case "90days":
                    startDate = now.AddDays(-90);
                    previousStartDate = startDate.AddDays(-90);
                    break;
                default: // 7days
                    startDate = now.AddDays(-7);
                    previousStartDate = startDate.AddDays(-7);
                    break;
            }

            var revenue = await _context.Orders
                .Where(o => o.CreatedAt >= startDate)
                .SumAsync(o => o.TotalAmount);

            var ordersCount = await _context.Orders
                .CountAsync(o => o.CreatedAt >= startDate);

            var customersCount = await _context.Users
                .CountAsync(u => u.CreatedAt >= startDate);

            var avgCart = ordersCount > 0 ? revenue / ordersCount : 0;

            var prevRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= previousStartDate && o.CreatedAt < startDate)
                .SumAsync(o => o.TotalAmount);
            
            var prevOrdersCount = await _context.Orders
                .CountAsync(o => o.CreatedAt >= previousStartDate && o.CreatedAt < startDate);

            var prevCustomersCount = await _context.Users
                .CountAsync(u => u.CreatedAt >= previousStartDate && u.CreatedAt < startDate);
            
            var prevAvgCart = prevOrdersCount > 0 ? prevRevenue / prevOrdersCount : 0;

            var revenueChange = prevRevenue > 0 ? (double)((revenue - prevRevenue) / prevRevenue * 100) : (revenue > 0 ? 100.0 : 0.0);
            var ordersChange = prevOrdersCount > 0 ? (double)(ordersCount - prevOrdersCount) / prevOrdersCount * 100 : (ordersCount > 0 ? 100.0 : 0.0);
            var customersChange = prevCustomersCount > 0 ? (double)(customersCount - prevCustomersCount) / prevCustomersCount * 100 : (customersCount > 0 ? 100.0 : 0.0);
            var avgCartChange = prevAvgCart > 0 ? (double)((avgCart - prevAvgCart) / prevAvgCart * 100) : (avgCart > 0 ? 100.0 : 0.0);

            var labels = new List<string>();
            var values = new List<double>();

            for (var date = startDate.Date; date <= now.Date; date = date.AddDays(1))
            {
                labels.Add(date.ToString("dd/MM"));
                var nextDate = date.AddDays(1);
                var dayRevenue = await _context.Orders
                    .Where(o => o.CreatedAt >= date && o.CreatedAt < nextDate)
                    .SumAsync(o => o.TotalAmount);
                values.Add((double)dayRevenue);
            }

            var result = new
            {
                metrics = new
                {
                    revenue = (double)revenue,
                    orders = ordersCount,
                    customers = customersCount,
                    avgCart = (double)avgCart,
                    revenueChange = revenueChange,
                    ordersChange = ordersChange,
                    customersChange = customersChange,
                    avgCartChange = avgCartChange
                },
                revenueData = new
                {
                    labels = labels,
                    values = values
                },
                ordersData = new
                {
                    labels = new[] { "Livré", "En attente", "Traitement", "Annulé" },
                    values = new[] { 
                        await _context.Orders.CountAsync(o => o.Status == "Delivered"),
                        await _context.Orders.CountAsync(o => o.Status == "Pending"),
                        await _context.Orders.CountAsync(o => o.Status == "Processing"),
                        await _context.Orders.CountAsync(o => o.Status == "Cancelled")
                    }
                },
                trafficData = new
                {
                    labels = new[] { "Direct", "Recherche", "Social", "Email" },
                    values = new[] { 40, 30, 20, 10 }
                },
                topProducts = await _context.OrderDetails
                    .Include(od => od.Product)
                    .GroupBy(od => new { od.ProductId, od.Product.Name })
                    .Select(g => new
                    {
                        id = g.Key.ProductId,
                        name = g.Key.Name,
                        sales = g.Sum(od => od.Quantity),
                        revenue = (double)g.Sum(od => od.UnitPrice * od.Quantity)
                    })
                    .OrderByDescending(x => x.sales)
                    .Take(5)
                    .ToListAsync()
            };

            return Ok(result);
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

                activeProducts = await _context.Products.CountAsync(p => p.IsActive),
                outOfStock = await _context.Products.CountAsync(p => p.StockQuantity == 0),
                lowStock = await _context.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 10),

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
                    p.StockQuantity,
                    p.Price,
                    p.CategoryId
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