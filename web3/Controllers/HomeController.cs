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
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Récupérer les produits en vedette
                var featuredProducts = await _context.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync();

                // Récupérer les catégories
                var categories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .Take(6)
                    .ToListAsync();

                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = featuredProducts,
                    Categories = categories,
                    BannerMessage = "Livraison gratuite à partir de 50€ d'achat!"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Index method");
                return View("Error", new Models.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        public async Task<IActionResult> Products(int? categoryId, string search = "", int page = 1)
        {
            try
            {
                var pageSize = 12;
                var query = _context.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .AsQueryable();

                if (categoryId.HasValue && categoryId > 0)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(searchLower) ||
                        (p.Description != null && p.Description.ToLower().Contains(searchLower)));
                }

                var totalProducts = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

                if (page < 1) page = 1;
                if (totalPages > 0 && page > totalPages) page = totalPages;

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
                    Products = products,
                    Categories = categories,
                    CurrentCategoryId = categoryId,
                    SearchTerm = search,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalProducts = totalProducts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Products method");
                return View("Error", new Models.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        public async Task<IActionResult> ProductDetails(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                {
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
                    RelatedProducts = relatedProducts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ProductDetails for ID: {id}");
                return View("Error", new Models.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        // Méthodes simples pour les pages statiques
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
                return View(model);
            }

            try
            {
                // Ici, vous pouvez ajouter la logique d'envoi d'email
                TempData["SuccessMessage"] = "Merci pour votre message! Nous vous répondrons dans les plus brefs délais.";
                return RedirectToAction("Contact");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Contact POST method");
                ModelState.AddModelError("", "Une erreur est survenue lors de l'envoi de votre message.");
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
            return View(new Models.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}