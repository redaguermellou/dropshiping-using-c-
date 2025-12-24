using ecom.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Produit")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "URL de l'image")]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        [Display(Name = "Alt text")]
        [StringLength(200)]
        public string AltText { get; set; }

        [Display(Name = "Ordre d'affichage")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Image principale")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Date d'ajout")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}