using Microsoft.AspNetCore.Mvc;
using ecom.Models;
using ecom.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ecom.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext context, ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                    .Include(c => c.Products.Where(p => p.IsActive))
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                return View("Error");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.ParentCategories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create category form");
                return View("Error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.CreatedAt = DateTime.UtcNow;
                    model.IsActive = true;

                    _context.Categories.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Catégorie créée avec succès!";
                    return RedirectToAction("Index");
                }

                ViewBag.ParentCategories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la création de la catégorie.";
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound();
                }

                ViewBag.ParentCategories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null && c.Id != id)
                    .ToListAsync();
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading category for edit ID: {id}");
                return View("Error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var category = await _context.Categories.FindAsync(model.Id);
                    if (category == null)
                    {
                        return NotFound();
                    }

                    category.Name = model.Name;
                    category.Description = model.Description;
                    category.ParentCategoryId = model.ParentCategoryId;
                    category.Icon = model.Icon;
                    category.IsActive = model.IsActive;

                    _context.Categories.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Catégorie mise à jour avec succès!";
                    return RedirectToAction("Index");
                }

                ViewBag.ParentCategories = await _context.Categories
                    .Where(c => c.IsActive && c.ParentCategoryId == null && c.Id != model.Id)
                    .ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating category ID: {model.Id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la mise à jour de la catégorie.";
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
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    TempData["ErrorMessage"] = "Catégorie introuvable.";
                    return RedirectToAction("Index");
                }

                // Vérifier si la catégorie a des sous-catégories actives
                if (category.SubCategories?.Any(sc => sc.IsActive) == true)
                {
                    TempData["ErrorMessage"] = "Impossible de supprimer cette catégorie car elle contient des sous-catégories actives.";
                    return RedirectToAction("Index");
                }

                // Vérifier si la catégorie a des produits actifs
                if (category.Products?.Any(p => p.IsActive) == true)
                {
                    TempData["ErrorMessage"] = "Impossible de supprimer cette catégorie car elle contient des produits actifs.";
                    return RedirectToAction("Index");
                }

                // Désactiver la catégorie au lieu de la supprimer
                category.IsActive = false;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Catégorie désactivée avec succès!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting category ID: {id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la suppression de la catégorie.";
                return RedirectToAction("Index");
            }
        }
    }
}