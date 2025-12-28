
using ecom.Models;

namespace ecom.Services
{
    public interface IAliExpressApiService
    {
        Task<Product> ImportProductFromUrl(string url);
    }

    public class DummyAliExpressService : IAliExpressApiService
    {
        private readonly ILogger<DummyAliExpressService> _logger;

        public DummyAliExpressService(ILogger<DummyAliExpressService> logger)
        {
            _logger = logger;
        }

        public Task<Product> ImportProductFromUrl(string url)
        {
            _logger.LogInformation("AliExpress service called with URL: {Url}", url);

            // Return a dummy product for testing
            return Task.FromResult(new Product
            {
                Name = "Sample Product from AliExpress",
                Description = "This is a sample product imported from AliExpress.",
                Price = 29.99m,
                SKU = $"AE-{DateTime.Now:yyyyMMddHHmmss}",
                ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800",
                Brand = "AliExpress"
            });
        }
    }
}