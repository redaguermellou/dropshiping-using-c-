using ecom.Data;
using ecom.Models;
using ecom.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Diagnostics;

namespace ecom.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            Debug.WriteLine($"[DEBUG] GET Login called. ReturnUrl: {returnUrl}");
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            try
            {
                Debug.WriteLine($"[DEBUG] POST Login attempt for: {model.EmailOrUsername}");

                if (!ModelState.IsValid)
                {
                    Debug.WriteLine($"[DEBUG] Login ModelState invalid");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Debug.WriteLine($"[DEBUG] Validation error: {error.ErrorMessage}");
                    }
                    return View(model);
                }

                Debug.WriteLine($"[DEBUG] Searching user with email/username: {model.EmailOrUsername}");

                // Rechercher l'utilisateur
                var user = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        (u.Email == model.EmailOrUsername || u.Username == model.EmailOrUsername) &&
                        u.IsActive);

                if (user == null)
                {
                    Debug.WriteLine($"[DEBUG] User not found: {model.EmailOrUsername}");
                    ModelState.AddModelError("", "Identifiants incorrects.");
                    return View(model);
                }

                Debug.WriteLine($"[DEBUG] User found: {user.Username}, ID: {user.Id}");

                // Vérifier le mot de passe (À REMPLACER PAR BCRYPT EN PRODUCTION)
                if (user.PasswordHash != model.Password)
                {
                    Debug.WriteLine($"[DEBUG] Password mismatch for user: {user.Username}");
                    ModelState.AddModelError("", "Identifiants incorrects.");
                    return View(model);
                }

                // Mettre à jour la dernière connexion
                user.LastLogin = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                Debug.WriteLine($"[DEBUG] LastLogin updated for user: {user.Id}");

                // Créer les claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FullName", $"{user.FirstName} {user.LastName}")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                Debug.WriteLine($"[DEBUG] User authenticated: {user.Username}, Claims set");

                _logger.LogInformation($"User {user.Username} logged in.");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    Debug.WriteLine($"[DEBUG] Redirecting to returnUrl: {returnUrl}");
                    return Redirect(returnUrl);
                }

                Debug.WriteLine($"[DEBUG] Redirecting to Home/Index");
                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateException dbEx)
            {
                Debug.WriteLine($"[ERROR] DbUpdateException in Login: {dbEx.Message}");
                _logger.LogError(dbEx, "Database error during login");
                ModelState.AddModelError("", "Une erreur de base de données est survenue.");
                return View(model);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Exception in Login: {ex.Message}");
                Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError("", "Une erreur est survenue. Veuillez réessayer.");
                return View(model);
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            Debug.WriteLine("[DEBUG] GET Register called");
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Debug.WriteLine("==========================================");
            Debug.WriteLine("[DEBUG] POST Register called");

            try
            {
                // Log des données reçues
                Debug.WriteLine($"[DEBUG] Received data:");
                Debug.WriteLine($"  FirstName: '{model.FirstName}'");
                Debug.WriteLine($"  LastName: '{model.LastName}'");
                Debug.WriteLine($"  Username: '{model.Username}'");
                Debug.WriteLine($"  Email: '{model.Email}'");
                Debug.WriteLine($"  Phone: '{model.Phone}'");
                Debug.WriteLine($"  Password: [HIDDEN], ConfirmPassword: [HIDDEN]");

                // Validation ModelState
                if (!ModelState.IsValid)
                {
                    Debug.WriteLine("[DEBUG] ModelState is INVALID");
                    foreach (var key in ModelState.Keys)
                    {
                        var errors = ModelState[key].Errors;
                        if (errors.Any())
                        {
                            foreach (var error in errors)
                            {
                                Debug.WriteLine($"  {key}: {error.ErrorMessage}");
                            }
                        }
                    }
                    return View(model);
                }

                Debug.WriteLine("[DEBUG] ModelState is VALID");

                // Nettoyer les données
                var username = model.Username?.Trim() ?? "";
                var email = model.Email?.Trim() ?? "";
                var firstName = model.FirstName?.Trim() ?? "";
                var lastName = model.LastName?.Trim() ?? "";
                var phone = model.Phone?.Trim() ?? "";

                Debug.WriteLine($"[DEBUG] Cleaned data: Username='{username}', Email='{email}'");

                // Vérification email
                Debug.WriteLine($"[DEBUG] Checking if email '{email}' exists...");
                var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
                Debug.WriteLine($"[DEBUG] Email exists: {emailExists}");

                if (emailExists)
                {
                    Debug.WriteLine($"[DEBUG] Email already exists, returning error");
                    ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                    return View(model);
                }

                // Vérification username
                Debug.WriteLine($"[DEBUG] Checking if username '{username}' exists...");
                var usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
                Debug.WriteLine($"[DEBUG] Username exists: {usernameExists}");

                if (usernameExists)
                {
                    Debug.WriteLine($"[DEBUG] Username already exists, returning error");
                    ModelState.AddModelError("Username", "Ce nom d'utilisateur est déjà pris.");
                    return View(model);
                }

                // Création de l'utilisateur
                Debug.WriteLine("[DEBUG] Creating new User object...");
                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = model.Password, // À hasher avec BCrypt en production
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = phone,
                    Role = "Customer",
                    IsActive = true,
                    EmailVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    Address = "", // Assurez-vous que ce n'est pas null
                    City = "",
                    PostalCode = "",
                    Country = "France"
                };

                Debug.WriteLine($"[DEBUG] User object created with properties:");
                Debug.WriteLine($"  Id: {user.Id}");
                Debug.WriteLine($"  Username: '{user.Username}'");
                Debug.WriteLine($"  Email: '{user.Email}'");
                Debug.WriteLine($"  FirstName: '{user.FirstName}'");
                Debug.WriteLine($"  LastName: '{user.LastName}'");
                Debug.WriteLine($"  Address: '{user.Address}'");
                Debug.WriteLine($"  City: '{user.City}'");
                Debug.WriteLine($"  PostalCode: '{user.PostalCode}'");
                Debug.WriteLine($"  Country: '{user.Country}'");
                Debug.WriteLine($"  CreatedAt: {user.CreatedAt}");

                // Tentative de sauvegarde
                Debug.WriteLine("[DEBUG] Adding user to context...");
                _context.Users.Add(user);

                Debug.WriteLine("[DEBUG] Calling SaveChangesAsync()...");
                var result = await _context.SaveChangesAsync();

                Debug.WriteLine($"[DEBUG] SaveChangesAsync completed. Result: {result}");
                Debug.WriteLine($"[DEBUG] User saved with ID: {user.Id}");

                // Connexion automatique
                Debug.WriteLine("[DEBUG] Creating claims for auto-login...");
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FullName", $"{user.FirstName} {user.LastName}")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                Debug.WriteLine("[DEBUG] Signing in user...");
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                Debug.WriteLine($"[SUCCESS] Registration completed in {sw.ElapsedMilliseconds}ms");
                Debug.WriteLine($"User registered: {user.Username} (ID: {user.Id})");

                TempData["SuccessMessage"] = "Inscription réussie! Bienvenue sur E-Shop.";
                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateException dbEx)
            {
                sw.Stop();
                Debug.WriteLine($"[DB ERROR] DbUpdateException after {sw.ElapsedMilliseconds}ms");
                Debug.WriteLine($"Message: {dbEx.Message}");

                var sqlEx = dbEx.InnerException as SqlException;
                if (sqlEx != null)
                {
                    Debug.WriteLine($"[SQL ERROR] Number: {sqlEx.Number}, State: {sqlEx.State}, LineNumber: {sqlEx.LineNumber}");
                    Debug.WriteLine($"[SQL ERROR] Message: {sqlEx.Message}");
                    Debug.WriteLine($"[SQL ERROR] Procedure: {sqlEx.Procedure}");

                    // Gestion des erreurs SQL spécifiques
                    switch (sqlEx.Number)
                    {
                        case 515: // Cannot insert the value NULL
                            Debug.WriteLine("[SQL ERROR] NULL insertion error - a required field is missing");
                            ModelState.AddModelError("", "Un champ requis est manquant. Assurez-vous que tous les champs sont remplis.");
                            break;
                        case 2627: // Violation de contrainte UNIQUE KEY
                        case 2601: // Violation de contrainte d'index unique
                            Debug.WriteLine("[SQL ERROR] Unique constraint violation");
                            ModelState.AddModelError("", "Cet email ou nom d'utilisateur existe déjà.");
                            break;
                        case 547: // Violation de contrainte FOREIGN KEY
                            Debug.WriteLine("[SQL ERROR] Foreign key constraint violation");
                            ModelState.AddModelError("", "Erreur de référence dans la base de données.");
                            break;
                        default:
                            Debug.WriteLine($"[SQL ERROR] Unknown SQL error: {sqlEx.Number}");
                            ModelState.AddModelError("", $"Erreur SQL: {sqlEx.Message}");
                            break;
                    }
                }
                else
                {
                    Debug.WriteLine($"[DB ERROR] Inner Exception: {dbEx.InnerException?.Message}");
                    ModelState.AddModelError("", $"Erreur base de données: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }

                _logger.LogError(dbEx, "Database error during registration");
                return View(model);
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"[ERROR] Exception after {sw.ElapsedMilliseconds}ms");
                Debug.WriteLine($"Type: {ex.GetType().Name}");
                Debug.WriteLine($"Message: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                _logger.LogError(ex, "Unexpected error during registration");
                ModelState.AddModelError("", $"Une erreur inattendue est survenue: {ex.Message}");
                return View(model);
            }
            finally
            {
                sw.Stop();
                Debug.WriteLine("==========================================");
            }
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            Debug.WriteLine("[DEBUG] Logout called");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["InfoMessage"] = "Vous avez été déconnecté.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            Debug.WriteLine("[DEBUG] GET Profile called");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                Debug.WriteLine("[DEBUG] No user ID in claims, redirecting to Login");
                return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });
            }

            Debug.WriteLine($"[DEBUG] User ID from claims: {userId}");

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                Debug.WriteLine($"[DEBUG] User not found in DB for ID: {userId}");
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login");
            }

            Debug.WriteLine($"[DEBUG] User found: {user.Username}");

            var model = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                City = user.City,
                PostalCode = user.PostalCode,
                Country = user.Country
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            try
            {
                Debug.WriteLine("[DEBUG] POST Profile update called");

                if (!ModelState.IsValid)
                {
                    Debug.WriteLine("[DEBUG] Profile ModelState invalid");
                    return View(model);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    Debug.WriteLine("[DEBUG] No user ID in profile update");
                    return RedirectToAction("Login");
                }

                Debug.WriteLine($"[DEBUG] Updating profile for user ID: {userId}");

                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    Debug.WriteLine($"[DEBUG] User not found for update: {userId}");
                    await HttpContext.SignOutAsync();
                    return RedirectToAction("Login");
                }

                // Vérifier si l'email a changé
                if (user.Email != model.Email && await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != int.Parse(userId)))
                {
                    Debug.WriteLine($"[DEBUG] Email already exists: {model.Email}");
                    ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                    return View(model);
                }

                // Mettre à jour
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.Address = model.Address;
                user.City = model.City;
                user.PostalCode = model.PostalCode;
                user.Country = model.Country;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                Debug.WriteLine($"[DEBUG] Profile updated for user: {user.Username}");

                // Mettre à jour le claim email si modifié
                if (user.Email != model.Email)
                {
                    var identity = (ClaimsIdentity)User.Identity;
                    var emailClaim = identity.FindFirst(ClaimTypes.Email);
                    if (emailClaim != null)
                    {
                        identity.RemoveClaim(emailClaim);
                    }
                    identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                    Debug.WriteLine($"[DEBUG] Email claim updated to: {user.Email}");
                }

                TempData["SuccessMessage"] = "Profil mis à jour avec succès!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Profile update error: {ex.Message}");
                _logger.LogError(ex, "Error updating profile");
                ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour.");
                return View(model);
            }
        }

        // GET: /Account/DebugTest
        [HttpGet]
        public async Task<IActionResult> DebugTest()
        {
            var output = new System.Text.StringBuilder();
            output.AppendLine("<h1>Debug Test Page</h1>");

            try
            {
                output.AppendLine("<h2>Database Connection Test</h2>");

                // Test de connexion
                var canConnect = await _context.Database.CanConnectAsync();
                output.AppendLine($"<p>Database can connect: <strong>{canConnect}</strong></p>");

                if (canConnect)
                {
                    // Test de la table Users
                    output.AppendLine("<h2>Users Table Test</h2>");

                    var userCount = await _context.Users.CountAsync();
                    output.AppendLine($"<p>Total users in database: <strong>{userCount}</strong></p>");

                    if (userCount > 0)
                    {
                        var sampleUser = await _context.Users.FirstOrDefaultAsync();
                        output.AppendLine($"<p>Sample user: {sampleUser?.Username} ({sampleUser?.Email})</p>");
                    }

                    // Test d'insertion simple
                    output.AppendLine("<h2>Test Insertion</h2>");
                    try
                    {
                        var testUser = new User
                        {
                            Username = "test_" + Guid.NewGuid().ToString().Substring(0, 8),
                            Email = "test_" + Guid.NewGuid().ToString().Substring(0, 8) + "@test.com",
                            PasswordHash = "test123",
                            FirstName = "Test",
                            LastName = "User",
                            Phone = "",
                            Role = "Customer",
                            IsActive = true,
                            EmailVerified = false,
                            CreatedAt = DateTime.UtcNow,
                            Address = "",
                            City = "",
                            PostalCode = "",
                            Country = "France"
                        };

                        _context.Users.Add(testUser);
                        var result = await _context.SaveChangesAsync();
                        output.AppendLine($"<p style='color:green'>Test insertion successful! Rows affected: {result}, User ID: {testUser.Id}</p>");

                        // Supprimer le test user
                        _context.Users.Remove(testUser);
                        await _context.SaveChangesAsync();
                        output.AppendLine($"<p>Test user removed.</p>");
                    }
                    catch (Exception ex)
                    {
                        output.AppendLine($"<p style='color:red'>Test insertion failed: {ex.Message}</p>");
                        if (ex.InnerException != null)
                        {
                            output.AppendLine($"<p>Inner: {ex.InnerException.Message}</p>");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                output.AppendLine($"<p style='color:red'>Debug test failed: {ex.Message}</p>");
            }

            return Content(output.ToString(), "text/html");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Debug.WriteLine("[DEBUG] AccessDenied called");
            return View();
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    }
}