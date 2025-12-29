using ecom.Data;
using ecom.Models;
using ecom.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ecom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Featured products
                var featuredProducts = await _context.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync();

                // Main categories
                var categories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .Take(6)
                    .ToListAsync();

                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = featuredProducts ?? new List<Product>(),
                    Categories = categories ?? new List<Category>(),
                    BannerMessage = "Free shipping on orders over $50!"
                };

                return View(viewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error in Index method");
                Console.WriteLine($"Database Error in Index: {dbEx.Message}");
                Console.WriteLine($"Inner Exception: {dbEx.InnerException?.Message}");
                return View("Error", new Models.ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Database connection error. Please try again later."
                });
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError(ioEx, "Invalid operation in Index method");
                Console.WriteLine($"Invalid Operation in Index: {ioEx.Message}");
                return View("Error", new Models.ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Service configuration error. Please contact support."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Index method");
                Console.WriteLine($"Unexpected Error in Index: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View("Error", new Models.ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "An unexpected error occurred. Please try again."
                });
            }
        }

        public async Task<IActionResult> Products(int? categoryId, string search = "", int page = 1)
        {
            try
            {
                const int pageSize = 12;

                IQueryable<Product> query = _context.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Category);

                // Filter by category
                if (categoryId.HasValue && categoryId > 0)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(searchLower) ||
                        (!string.IsNullOrEmpty(p.Description) && p.Description.ToLower().Contains(searchLower)));
                }

                // Pagination
                var totalProducts = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

                page = Math.Clamp(page, 1, totalPages > 0 ? totalPages : 1);

                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();

                var viewModel = new ProductsViewModel
                {
                    Products = products ?? new List<Product>(),
                    Categories = categories ?? new List<Category>(),
                    CurrentCategoryId = categoryId,
                    SearchTerm = search,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalProducts = totalProducts
                };

                return View(viewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error in Products method");
                Console.WriteLine($"Database Error in Products: {dbEx.Message}");
                Console.WriteLine($"Inner Exception: {dbEx.InnerException?.Message}");
                ModelState.AddModelError("", "Database error occurred while retrieving products.");
                return View("Error", new Models.ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Unable to load products. Please try again later."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Products method");
                Console.WriteLine($"Unexpected Error in Products: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View("Error", new Models.ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "An error occurred while loading products."
                });
            }
        }

        public async Task<IActionResult> ProductDetails(int id)
        {
            try
            {
                if (id <= 0)
                {
                    Console.WriteLine($"Invalid product ID requested: {id}");
                    return NotFound();
                }

                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                {
                    Console.WriteLine($"Product not found with ID: {id}");
                    return NotFound();
                }

                var relatedProducts = await _context.Products
                    .Where(p => p.CategoryId == product.CategoryId &&
                                p.Id != id &&
                                p.IsActive)
                    .Take(4)
                    .ToListAsync();

                var viewModel = new ProductDetailViewModel
                {
                    Product = product,
                    RelatedProducts = relatedProducts ?? new List<Product>()
                };

                return View(viewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error in ProductDetails for ID: {id}");
                Console.WriteLine($"Database Error in ProductDetails: {dbEx.Message}");
                return View("Error", new Models.ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Database error while retrieving product details."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error in ProductDetails for ID: {id}");
                Console.WriteLine($"Unexpected Error in ProductDetails: {ex.Message}");
                return View("Error", new Models.ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "An error occurred while loading product details."
                });
            }
        }

        // Simple methods for static pages
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"Contact form validation failed. Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return View(model);
            }

            try
            {
                // Here you can add email sending logic
                Console.WriteLine($"Contact form submitted successfully. Name: {model.Name}, Email: {model.Email}");

                TempData["SuccessMessage"] = "Thank you for your message! We will respond as soon as possible.";
                return RedirectToAction("Contact");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Contact POST method");
                Console.WriteLine($"Error sending contact form: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while sending your message.");
                return View(model);
            }
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            Console.WriteLine($"Error page accessed. Request ID: {Activity.Current?.Id ?? HttpContext.TraceIdentifier}");
            return View(new Models.ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = "An error occurred while processing your request."
            });
        }

        // Additional diagnostic action for testing
        public IActionResult Diagnostic()
        {
            try
            {
                Console.WriteLine("Diagnostic endpoint called");

                var dbContextInfo = new
                {
                    IsContextNull = _context == null,
                    CanConnect = _context.Database.CanConnect(),
                    ProviderName = _context.Database.ProviderName,
                    CategoriesCount = _context.Categories?.Count(),
                    ProductsCount = _context.Products?.Count()
                };

                Console.WriteLine($"DbContext Info: {System.Text.Json.JsonSerializer.Serialize(dbContextInfo)}");

                return Json(new
                {
                    Status = "OK",
                    Timestamp = DateTime.UtcNow,
                    DbContext = dbContextInfo,
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Diagnostic Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Json(new
                {
                    Status = "ERROR",
                    Message = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
            public async Task<IActionResult> SeedAdmin()
        {
            try {
                var adminEmail = "admin@shopnex.com";
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ecom.Models.User
                    {
                        Username = "admin",
                        Email = adminEmail,
                        PasswordHash = "Admin123!",
                        FirstName = "Admin",
                        LastName = "System",
                        Phone = "0000000000",
                        Role = "Admin",
                        IsActive = true,
                        EmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        Address = "System",
                        City = "System",
                        PostalCode = "00000",
                        Country = "System"
                    };
                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();
                    return Content("Admin user created: admin@shopnex.com / Admin123!");
                }
                return Content("Admin user already exists.");
            } catch (Exception ex) {
                return Content("Error: " + ex.Message);
            }
        }
    }
}
