using Microsoft.AspNetCore.Mvc;
using ecom.Models;
using ecom.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ecom.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                return View("Error");
            }
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                // Vérifier l'autorisation
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out int parsedUserId))
                {
                    if (order.UserId != parsedUserId && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading order details ID: {id}");
                return View("Error");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Users = await _context.Users.ToListAsync();
                ViewBag.Products = await _context.Products.Where(p => p.IsActive).ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create order form");
                return View("Error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.CreatedAt = DateTime.UtcNow;
                    model.OrderNumber ??= GenerateOrderNumber();

                    _context.Orders.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Commande créée avec succès!";
                    return RedirectToAction("Index");
                }

                ViewBag.Users = await _context.Users.ToListAsync();
                ViewBag.Products = await _context.Products.Where(p => p.IsActive).ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la création de la commande.";
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> History()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out int parsedUserId))
                {
                    var orders = await _context.Orders
                        .Where(o => o.UserId == parsedUserId)
                        .OrderByDescending(o => o.CreatedAt)
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                        .ToListAsync();

                    return View(orders);
                }

                return View(new List<Order>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order history");
                return View("Error");
            }
        }

        [Authorize]
        public async Task<IActionResult> Confirmation(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                // Vérifier l'autorisation
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out int parsedUserId))
                {
                    if (order.UserId != parsedUserId && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading order confirmation ID: {id}");
                return View("Error");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Commande introuvable.";
                    return RedirectToAction("History");
                }

                // Vérifier l'autorisation
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out int parsedUserId))
                {
                    if (order.UserId != parsedUserId && !User.IsInRole("Admin"))
                    {
                        TempData["ErrorMessage"] = "Vous n'êtes pas autorisé à annuler cette commande.";
                        return RedirectToAction("History");
                    }
                }

                // Vérifier si la commande peut être annulée
                if (order.Status == "Pending" || order.Status == "Confirmed")
                {
                    order.Status = "Cancelled";
                    order.UpdatedAt = DateTime.UtcNow;

                    // Restaurer le stock des produits
                    var orderDetails = await _context.OrderDetails
                        .Where(od => od.OrderId == id)
                        .Include(od => od.Product)
                        .ToListAsync();

                    foreach (var detail in orderDetails)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.StockQuantity += detail.Quantity;
                            detail.Product.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Commande annulée avec succès.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Cette commande ne peut plus être annulée.";
                }

                return RedirectToAction("History");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling order ID: {id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de l'annulation de la commande.";
                return RedirectToAction("History");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            try
            {
                var validStatuses = new[] { "Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled" };

                if (!validStatuses.Contains(status))
                {
                    TempData["ErrorMessage"] = "Statut invalide.";
                    return RedirectToAction("Index");
                }

                var order = await _context.Orders.FindAsync(id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Commande introuvable.";
                    return RedirectToAction("Index");
                }

                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;

                // Mettre à jour les dates spécifiques
                if (status == "Shipped")
                {
                    order.ShippedAt = DateTime.UtcNow;
                }
                else if (status == "Delivered")
                {
                    order.DeliveredAt = DateTime.UtcNow;
                    order.PaymentStatus = "Paid"; // Marquer comme payé à la livraison
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Statut de la commande mis à jour: {status}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order status ID: {id}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la mise à jour du statut.";
                return RedirectToAction("Index");
            }
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }
    }
}