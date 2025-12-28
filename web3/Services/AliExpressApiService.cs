// Services/AliExpressApiService.cs
using ecom.Models;
using Newtonsoft.Json;

namespace ecom.Services
{
    public interface IAliExpressApiService
    {
        Task<Product> ImportProductFromUrl(string productUrl);
        Task<decimal> GetProductPrice(string productUrl);
    }

    public class ZendropApiService : IAliExpressApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ZendropApiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Zendrop:ApiKey"];
        }

        public Task<decimal> GetProductPrice(string productUrl)
        {
            throw new NotImplementedException();
        }

        public async Task<Product> ImportProductFromUrl(string productUrl)
        {
            try
            {
                // Exemple avec Zendrop API
                var response = await _httpClient.GetAsync(
                    $"https://api.zendrop.com/v1/products/import?url={Uri.EscapeDataString(productUrl)}&api_key={_apiKey}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiProduct = JsonConvert.DeserializeObject<ZendropProduct>(content);

                    // Convertir en votre modèle Product
                    return new Product
                    {
                        Name = apiProduct.Title,
                        Description = apiProduct.Description,
                        Price = CalculatePriceWithMargin(apiProduct.Price),
                        SKU = apiProduct.Sku ?? GenerateSku(),
                        StockQuantity = 100, // Par défaut
                        ImageUrl = apiProduct.Images?.FirstOrDefault(),
                        Brand = "AliExpress",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception)
            {
                // Gérer l'erreur
            }

            return null;
        }

        private decimal CalculatePriceWithMargin(decimal costPrice)
        {
            // Ajouter 30% de marge + conversion USD à EUR
            decimal priceInEur = costPrice * 0.85m; // Conversion
            decimal margin = priceInEur * 0.30m; // 30% de marge
            return Math.Round(priceInEur + margin, 2);
        }

        private string GenerateSku()
        {
            return $"AE-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }

    // Modèle pour la réponse de l'API Zendrop
    public class ZendropProduct
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Sku { get; set; }
        public List<string> Images { get; set; }
    }
}