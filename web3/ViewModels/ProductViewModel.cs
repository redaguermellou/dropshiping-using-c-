using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ecom.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Le prix est requis")]
        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "La quantité est requise")]
        [Range(0, 10000)]
        public int StockQuantity { get; set; }

        public string SKU { get; set; }

        [Required(ErrorMessage = "La catégorie est requise")]
        [Display(Name = "Catégorie")]
        public int CategoryId { get; set; }

        public string Brand { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Image")]
        public IFormFile ImageFile { get; set; }

        public string ExistingImageUrl { get; set; }

        public List<SelectListItem> Categories { get; set; }
    }
}