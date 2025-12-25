using Microsoft.AspNetCore.Mvc;
using ecom.Data;
using ecom.Models;
using ecom.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ecom.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetUserId();
                var cart = await GetOrCreateCartAsync(userId);

                // Ensure all cart items have product data loaded
                if (cart != null && cart.Items?.Any() == true)
                {
                    foreach (var item in cart.Items)
                    {
                        if (item.Product == null)
                        {
                            await _context.Entry(item)
                                .Reference(i => i.Product)
                                .LoadAsync();
                        }
                    }
                }

                var totalAmount = cart?.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0;
                var totalAmountWithVat = totalAmount * 1.2m;

                var viewModel = new CartViewModel
                {
                    Cart = cart,
                    TotalAmount = totalAmount,
                    TotalAmountWithVat = totalAmountWithVat
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors du chargement du panier.";
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            try
            {
                if (quantity < 1)
                {
                    TempData["ErrorMessage"] = "La quantité doit être au moins 1.";
                    return RedirectToAction("Products", "Home");
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Produit introuvable.";
                    return RedirectToAction("Products", "Home");
                }

                if (product.StockQuantity <= 0 || quantity > product.StockQuantity)
                {
                    TempData["ErrorMessage"] = "Stock insuffisant.";
                    return RedirectToAction("ProductDetails", "Home", new { id = productId });
                }

                var userId = GetUserId();
                var cart = await GetOrCreateCartAsync(userId);

                // Vérifier si l'article existe déjà dans le panier
                var existingItem = cart.Items?.FirstOrDefault(i => i.ProductId == productId);

                if (existingItem != null)
                {
                    var newTotalQuantity = existingItem.Quantity + quantity;

                    // Check stock with new total quantity
                    if (newTotalQuantity > product.StockQuantity)
                    {
                        TempData["ErrorMessage"] = "Stock insuffisant pour la quantité demandée.";
                        return RedirectToAction("ProductDetails", "Home", new { id = productId });
                    }

                    existingItem.Quantity = newTotalQuantity;
                    existingItem.UnitPrice = product.Price;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price,
                        AddedAt = DateTime.UtcNow
                    };

                    _context.CartItems.Add(cartItem);
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Produit ajouté au panier avec succès!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding product {productId} to cart");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de l'ajout au panier.";
                return RedirectToAction("ProductDetails", "Home", new { id = productId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            try
            {
                if (quantity < 1)
                {
                    TempData["ErrorMessage"] = "La quantité doit être au moins 1.";
                    return RedirectToAction("Index");
                }

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

                if (cartItem == null)
                {
                    TempData["ErrorMessage"] = "Article introuvable dans le panier.";
                    return RedirectToAction("Index");
                }

                // Vérifier le stock
                if (quantity > cartItem.Product?.StockQuantity)
                {
                    TempData["ErrorMessage"] = "Stock insuffisant.";
                    return RedirectToAction("Index");
                }

                cartItem.Quantity = quantity;

                // Update cart timestamp
                var cart = await _context.Carts.FindAsync(cartItem.CartId);
                if (cart != null)
                {
                    cart.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Quantité mise à jour.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating cart item {cartItemId}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la mise à jour.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);

                    // Update cart timestamp
                    if (cartItem.Cart != null)
                    {
                        cartItem.Cart.UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Produit retiré du panier.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Article introuvable dans le panier.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing cart item {cartItemId}");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la suppression.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            try
            {
                var userId = GetUserId();
                var cart = await GetCartAsync(userId);

                if (cart != null && cart.Items?.Any() == true)
                {
                    _context.CartItems.RemoveRange(cart.Items);
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Panier vidé avec succès.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors du vidage du panier.";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Convertir l'ID utilisateur string en int
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var cart = await GetCartAsync(userId.ToString());

                if (cart == null || cart.Items?.Any() != true)
                {
                    TempData["ErrorMessage"] = "Votre panier est vide.";
                    return RedirectToAction("Index");
                }

                // Ensure all products are loaded
                foreach (var item in cart.Items)
                {
                    if (item.Product == null)
                    {
                        await _context.Entry(item)
                            .Reference(i => i.Product)
                            .LoadAsync();
                    }
                }

                // Vérifier la disponibilité des produits
                var unavailableItems = new List<string>();
                foreach (var item in cart.Items)
                {
                    if (item.Product == null || item.Quantity > item.Product.StockQuantity)
                    {
                        unavailableItems.Add(item.Product?.Name ?? $"Produit ID: {item.ProductId}");
                    }
                }

                if (unavailableItems.Any())
                {
                    TempData["ErrorMessage"] = $"Les produits suivants ne sont plus disponibles en quantité suffisante: {string.Join(", ", unavailableItems)}";
                    return RedirectToAction("Index");
                }

                // Calculer les totaux
                var totalAmount = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
                var totalAmountWithVat = totalAmount * 1.2m;

                // Get user for pre-filling checkout form
                var user = await _context.Users.FindAsync(userId);

                var viewModel = new CheckoutViewModel
                {
                    Cart = cart,
                    TotalAmount = totalAmount,
                    TotalAmountWithVat = totalAmountWithVat,
                    Email = user?.Email ?? string.Empty,
                   
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors du chargement de la page de paiement.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Recharger les données nécessaires
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        model.Cart = await GetCartAsync(userId.ToString());
                        if (model.Cart != null && model.Cart.Items?.Any() == true)
                        {
                            model.TotalAmount = model.Cart.Items.Sum(i => i.UnitPrice * i.Quantity);
                            model.TotalAmountWithVat = model.TotalAmount * 1.2m;
                        }
                    }
                    return View(model);
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int parsedUserId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var cart = await GetCartAsync(userIdString);

                if (cart == null || cart.Items?.Any() != true)
                {
                    TempData["ErrorMessage"] = "Votre panier est vide.";
                    return RedirectToAction("Index");
                }

                // Ensure all products are loaded
                foreach (var item in cart.Items)
                {
                    if (item.Product == null)
                    {
                        await _context.Entry(item)
                            .Reference(i => i.Product)
                            .LoadAsync();
                    }
                }

                // Vérifier à nouveau la disponibilité
                var unavailableItems = new List<string>();
                foreach (var item in cart.Items)
                {
                    if (item.Product == null || item.Quantity > item.Product.StockQuantity)
                    {
                        unavailableItems.Add(item.Product?.Name ?? $"Produit ID: {item.ProductId}");
                    }
                }

                if (unavailableItems.Any())
                {
                    TempData["ErrorMessage"] = $"Les produits suivants ne sont plus disponibles en quantité suffisante: {string.Join(", ", unavailableItems)}";
                    return RedirectToAction("Index");
                }

                // Use transaction for data consistency
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Créer la commande
                    var order = new Order
                    {
                        UserId = parsedUserId,
                        OrderNumber = GenerateOrderNumber(),
                        TotalAmount = model.TotalAmount,
                        Status = "Pending",
                        ShippingAddress = model.ShippingAddress,
                        BillingAddress = model.ShippingAddress, // Same as shipping for now
                        PaymentMethod = model.PaymentMethod,
                        PaymentStatus = "Pending",
                        Notes = model.Notes,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Sauvegarder pour obtenir l'ID

                    // Ajouter les détails de la commande
                    foreach (var cartItem in cart.Items)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = cartItem.ProductId,
                            Quantity = cartItem.Quantity,
                            UnitPrice = cartItem.UnitPrice,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.OrderDetails.Add(orderDetail);

                        // Mettre à jour le stock
                        var product = await _context.Products.FindAsync(cartItem.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity -= cartItem.Quantity;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    // Vider le panier
                    _context.CartItems.RemoveRange(cart.Items);

                    // Update cart timestamp
                    cart.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Commande créée avec succès!";
                    return RedirectToAction("Confirmation", "Order", new { id = order.Id });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la validation de votre commande.";

                // Recharger les données
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out int parsedUserId))
                {
                    model.Cart = await GetCartAsync(userId);
                    if (model.Cart != null && model.Cart.Items?.Any() == true)
                    {
                        model.TotalAmount = model.Cart.Items.Sum(i => i.UnitPrice * i.Quantity);
                        model.TotalAmountWithVat = model.TotalAmount * 1.2m;
                    }
                }
                return View(model);
            }
        }

        // Méthodes d'aide privées
        private async Task<Cart> GetOrCreateCartAsync(string userId)
        {
            Cart cart = null;

            if (int.TryParse(userId, out int parsedUserId))
            {
                // Pour les utilisateurs authentifiés
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == parsedUserId);
            }
            else
            {
                // Pour les utilisateurs anonymes
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == userId);
            }

            if (cart == null)
            {
                cart = new Cart
                {
                    CreatedAt = DateTime.UtcNow
                };

                if (int.TryParse(userId, out int uid))
                {
                    cart.UserId = uid;
                }
                else
                {
                    cart.SessionId = userId;
                }

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                // Reload with includes
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.Id == cart.Id);
            }

            return cart ?? new Cart();
        }

        private async Task<Cart> GetCartAsync(string userId)
        {
            if (int.TryParse(userId, out int parsedUserId))
            {
                // Utilisateur authentifié
                return await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == parsedUserId);
            }
            else
            {
                // Utilisateur anonyme
                return await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == userId);
            }
        }

        private string GetUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            // Pour les utilisateurs non authentifiés, utiliser l'ID de session
            var sessionId = HttpContext.Session.GetString("CartSessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("CartSessionId", sessionId);
            }

            return sessionId;
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }

        // AJAX methods for better UX
        [HttpGet]
        public async Task<JsonResult> GetCartSummary()
        {
            try
            {
                var userId = GetUserId();
                var cart = await GetCartAsync(userId);

                var itemCount = cart?.Items?.Sum(i => i.Quantity) ?? 0;
                var totalAmount = cart?.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0;

                return Json(new
                {
                    success = true,
                    itemCount,
                    totalAmount = totalAmount.ToString("C2"),
                    totalItems = itemCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary");
                return Json(new { success = false, message = "Erreur lors du chargement du panier." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateQuantityAjax(int cartItemId, int quantity)
        {
            try
            {
                if (quantity < 1)
                {
                    return Json(new { success = false, message = "La quantité doit être au moins 1." });
                }

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Article introuvable." });
                }

                // Vérifier le stock
                if (quantity > cartItem.Product?.StockQuantity)
                {
                    return Json(new { success = false, message = "Stock insuffisant." });
                }

                cartItem.Quantity = quantity;

                // Update cart timestamp
                var cart = await _context.Carts.FindAsync(cartItem.CartId);
                if (cart != null)
                {
                    cart.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Recalculate totals
                var updatedCart = await GetCartAsync(GetUserId());
                var itemCount = updatedCart?.Items?.Sum(i => i.Quantity) ?? 0;
                var totalAmount = updatedCart?.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0;
                var subtotal = cartItem.UnitPrice * quantity;

                return Json(new
                {
                    success = true,
                    message = "Quantité mise à jour.",
                    subtotal = subtotal.ToString("C2"),
                    totalAmount = totalAmount.ToString("C2"),
                    totalAmountWithVat = (totalAmount * 1.2m).ToString("C2"),
                    itemCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating cart item {cartItemId}");
                return Json(new { success = false, message = "Erreur lors de la mise à jour." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> RemoveItemAjax(int cartItemId)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);

                    // Update cart timestamp
                    if (cartItem.Cart != null)
                    {
                        cartItem.Cart.UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                }

                // Recalculate totals
                var cart = await GetCartAsync(GetUserId());
                var itemCount = cart?.Items?.Sum(i => i.Quantity) ?? 0;
                var totalAmount = cart?.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0;

                return Json(new
                {
                    success = true,
                    message = "Produit retiré.",
                    totalAmount = totalAmount.ToString("C2"),
                    totalAmountWithVat = (totalAmount * 1.2m).ToString("C2"),
                    itemCount,
                    isEmpty = itemCount == 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing cart item {cartItemId}");
                return Json(new { success = false, message = "Erreur lors de la suppression." });
            }
        }
    }
}