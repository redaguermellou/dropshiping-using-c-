// Services/MockAliExpressService.cs
using ecom.Models;
using ecom.Services;

public class MockAliExpressService : IAliExpressApiService
{
    public Task<Product> ImportProductFromUrl(string productUrl)
    {
        // Simuler un produit pour le test
        var product = new Product
        {
            Name = "Produit AliExpress de test",
            Description = "Description détaillée du produit importé depuis AliExpress.",
            Price = 49.99m,
            SKU = $"AE-TEST-{DateTime.Now.Ticks}",
            ImageUrl = "https://via.placeholder.com/300",
            CreatedAt = DateTime.UtcNow
        };

        return Task.FromResult(product);
    }

    public Task<decimal> GetProductPrice(string productUrl)
    {
        return Task.FromResult(29.99m);
    }
}