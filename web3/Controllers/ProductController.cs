using ecom.Data;
using ecom.Models;
using ecom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ecom.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int? categoryId, string search, int page = 1)
        {
            const int pageSize = 12;

            try
            {
                var query = _context.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .AsQueryable();

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(search) ||
                        p.Description.ToLower().Contains(search) ||
                        (p.Brand != null && p.Brand.ToLower().Contains(search)));
                }

                var totalProducts = await query.CountAsync();
                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();
                ViewBag.CurrentCategoryId = categoryId;
                ViewBag.SearchTerm = search;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
                ViewBag.TotalProducts = totalProducts;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products list");
                return View("Error");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
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

                // Charger les produits similaires
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
                _logger.LogError(ex, $"Error loading product details for ID: {id}");
                return View("Error");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create product form");
                return View("Error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validation supplémentaire
                    if (model.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Le prix doit être supérieur à 0.");
                    }

                    if (model.StockQuantity < 0)
                    {
                        ModelState.AddModelError("StockQuantity", "La quantité en stock ne peut pas être négative.");
                    }

                    if (ModelState.IsValid)
                    {
                        model.CreatedAt = DateTime.UtcNow;
                        model.IsActive = true;
                        model.SKU ??= $"PROD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

                        _context.Products.Add(model);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Produit créé avec succès!";
                        return RedirectToAction("Index");
                    }
                }

                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la création du produit.";
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                {
                    return NotFound();
                }

                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading product for edit ID: {id}");
                return View("Error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Le prix doit être supérieur à 0.");
                    }

                    if (model.StockQuantity < 0)
                    {
                        ModelState.AddModelError("StockQuantity", "La quantité en stock ne peut pas être négative.");
                    }

                    if (ModelState.IsValid)
                    {
                        model.UpdatedAt = DateTime.UtcNow;

                        _context.Products.Update(model);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Produit mis à jour avec succès!";
                        return RedirectToAction("Index");
                    }
                }

                ViewBag.Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating product ID: {model.Id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la mise à jour du produit.";
                return View(model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product != null)
                {
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.UtcNow;

                    _context.Products.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Produit désactivé avec succès!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Produit introuvable.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting product ID: {id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la suppression du produit.";
                return RedirectToAction("Index");
            }
        }
    }
}