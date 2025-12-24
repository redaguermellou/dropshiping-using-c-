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

                var totalAmount = cart.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0;
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
                    existingItem.Quantity += quantity;
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
                if (quantity > cartItem.Product.StockQuantity)
                {
                    TempData["ErrorMessage"] = "Stock insuffisant.";
                    return RedirectToAction("Index");
                }

                cartItem.Quantity = quantity;
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
                var cartItem = await _context.CartItems.FindAsync(cartItemId);

                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);
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

                // Vérifier la disponibilité des produits
                foreach (var item in cart.Items)
                {
                    await _context.Entry(item)
                        .Reference(i => i.Product)
                        .LoadAsync();

                    if (item.Product == null || item.Quantity > item.Product.StockQuantity)
                    {
                        TempData["ErrorMessage"] = $"Le produit {item.Product?.Name} n'est plus disponible en quantité suffisante.";
                        return RedirectToAction("Index");
                    }
                }

                // Calculer les totaux
                var totalAmount = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
                var totalAmountWithVat = totalAmount * 1.2m;

                var viewModel = new CheckoutViewModel
                {
                    Cart = cart,
                    TotalAmount = totalAmount,
                    TotalAmountWithVat = totalAmountWithVat
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                return View("Error");
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

                // Vérifier à nouveau la disponibilité
                foreach (var item in cart.Items)
                {
                    await _context.Entry(item)
                        .Reference(i => i.Product)
                        .LoadAsync();

                    if (item.Quantity > item.Product.StockQuantity)
                    {
                        TempData["ErrorMessage"] = $"Le produit {item.Product.Name} n'est plus disponible en quantité suffisante.";
                        return RedirectToAction("Index");
                    }
                }

                // Créer la commande
                var order = new Order
                {
                    UserId = parsedUserId,
                    OrderNumber = GenerateOrderNumber(),
                   
                    ShippingAddress = model.ShippingAddress,
                   
                    PaymentMethod = model.PaymentMethod,
                    Notes = model.Notes,
                    Status = "Pending",
                    PaymentStatus = "Pending",
                    TotalAmount = model.TotalAmount,
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
                        _context.Products.Update(product);
                    }
                }

                // Vider le panier
                _context.CartItems.RemoveRange(cart.Items);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Commande créée avec succès!";
                return RedirectToAction("Confirmation", "Order", new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                TempData["ErrorMessage"] = "Une erreur s'est produite lors de la validation de votre commande.";

                // Recharger les données
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.Cart = await GetCartAsync(userId);
                return View(model);
            }
        }

        // Méthodes d'aide privées
        private async Task<Cart> GetOrCreateCartAsync(string userId)
        {
            if (!int.TryParse(userId, out int parsedUserId))
            {
                // Pour les utilisateurs anonymes, utiliser le SessionId
                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == userId);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        SessionId = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                return cart;
            }

            // Pour les utilisateurs authentifiés
            var userCart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == parsedUserId);

            if (userCart == null)
            {
                userCart = new Cart
                {
                    UserId = parsedUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(userCart);
                await _context.SaveChangesAsync();
            }

            return userCart;
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
    }
}