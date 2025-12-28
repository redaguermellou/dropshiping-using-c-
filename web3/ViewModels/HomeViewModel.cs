using ecom.Models;

using System.ComponentModel.DataAnnotations;

namespace ecom.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; }
        public List<Category> Categories { get; set; }
        public string BannerMessage { get; set; }
    }

    public class ProductsViewModel
    {
        public List<Product> Products { get; set; }
        public List<Category> Categories { get; set; }
        public int? CurrentCategoryId { get; set; }
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }
    }

    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<Product> RelatedProducts { get; set; }
    }

    public class ContactViewModel
    {
        [Required(ErrorMessage = "Le nom est requis")]
        public string Name { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le sujet est requis")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Le message est requis")]
        public string Message { get; set; }
    }

    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}