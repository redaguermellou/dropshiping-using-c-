using ecom.Data;
using ecom.Models;
using ecom.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ecom.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var cart = await GetCartAsync();
            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (quantity <= 0) quantity = 1;

            var cart = await GetCartAsync();

            // Use the model method to add item
            cart.AddItem(product, quantity);

            // If the cart is new (Id == 0), Add it to context
            if (cart.Id == 0)
            {
                _context.Carts.Add(cart);
            }
            // If it exists, update it to track changes
            else
            {
                _context.Carts.Update(cart);
            }

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { count = cart.ItemCount });
            }

            TempData["Success"] = "Produit ajouté au panier avec succès";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var cart = await GetCartAsync();

            cart.RemoveItem(productId);
            
            // Explicitly set UpdatedAt since we modified the cart
            cart.UpdatedAt = DateTime.UtcNow;
            
            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Produit retiré du panier";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 1)
            {
                return await RemoveFromCart(productId);
            }

            var cart = await GetCartAsync();
            
            // Check stock if needed
            var product = await _context.Products.FindAsync(productId);
            if (product != null && quantity > product.StockQuantity)
            {
                TempData["Error"] = $"Désolé, seulement {product.StockQuantity} articles disponibles en stock.";
                return RedirectToAction(nameof(Index));
            }

            cart.UpdateQuantity(productId, quantity);

            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Quantité mise à jour";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var cart = await GetCartAsync();
            return Json(new { count = cart.ItemCount });
        }

        // GET: /Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            }

            var cart = await GetCartAsync();
            if (cart == null || cart.IsEmpty)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var viewModel = new CheckoutViewModel
            {
                Cart = cart,
                TotalAmount = cart.TotalAmount,
                TotalAmountWithVat = cart.TotalAmountWithVat,
                Email = user?.Email,
                Phone = user?.Phone,
                ShippingAddress = user?.Address,
                City = user?.City,
                PostalCode = user?.PostalCode,
                Country = user?.Country ?? "France"
            };

            return View(viewModel);
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await GetCartAsync();
            if (cart == null || cart.IsEmpty)
            {
                ModelState.AddModelError("", "Votre panier est vide.");
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create Order
                    var order = new Order
                    {
                        OrderNumber = "ORD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        UserId = userId.Value,
                        TotalAmount = cart.TotalAmountWithVat,
                        Status = "Pending",
                        ShippingAddress = model.ShippingAddress + ", " + model.PostalCode + " " + model.City + ", " + model.Country,
                        PaymentMethod = model.PaymentMethod,
                        PaymentStatus = "Pending",
                        Notes = model.Notes,
                        CreatedAt = DateTime.UtcNow,
                        OrderDetails = new List<OrderDetail>()
                    };

                    foreach (var item in cart.Items)
                    {
                        order.OrderDetails.Add(new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });

                        // Optionally update stock
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity -= item.Quantity;
                        }
                    }

                    _context.Orders.Add(order);
                    
                    // Clear cart
                    cart.Clear();
                    _context.Carts.Update(cart);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Commande passée avec succès !";
                    return RedirectToAction("Confirmation", "Order", new { id = order.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Une erreur est survenue lors de la validation de votre commande.");
                }
            }

            model.Cart = cart;
            return View(model);
        }

        private async Task<Cart> GetCartAsync()
        {
            Cart cart = null;
            var userId = GetUserId();
            var sessionId = GetSessionId();

            if (userId.HasValue)
            {
                // Authenticated user: Load by UserId
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId.Value,
                        CreatedAt = DateTime.UtcNow,
                        SessionId = sessionId // Optional: link session ID too
                    };
                }
            }
            else
            {
                // Guest user: Load by SessionId
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        SessionId = sessionId,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }

            return cart;
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            // Fallback: checks for custom claim name "Id" if standard Type is not used
            var customIdClaim = User.FindFirst("Id"); 
             if (customIdClaim != null && int.TryParse(customIdClaim.Value, out int customId))
            {
                return customId;
            }

            return null;
        }

        private string GetSessionId()
        {
            var session = HttpContext.Session;
            string sessionId = session.GetString("CartSessionId");

            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                session.SetString("CartSessionId", sessionId);
            }

            return sessionId;
        }
    }
}
