// Controllers/Api/AliExpressController.cs
using Microsoft.AspNetCore.Mvc;
using ecom.Services;

namespace ecom.Controllers.Api
{
    [Route("api/aliexpress")]
    [ApiController]
    public class AliExpressApiController : ControllerBase
    {
        private readonly IAliExpressApiService _apiService;

        public AliExpressApiController(IAliExpressApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportProduct([FromBody] ImportRequest request)
        {
            try
            {
                var product = await _apiService.ImportProductFromUrl(request.Url);

                if (product == null)
                {
                    return BadRequest(new { success = false, message = "Impossible d'importer le produit" });
                }

                return Ok(new
                {
                    success = true,
                    product = new
                    {
                        product.Name,
                        product.Description,
                        product.Price,
                        product.SKU,
                        product.ImageUrl
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public class ImportRequest
        {
            public string Url { get; set; }
        }
    }
}