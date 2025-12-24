using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ecom.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom du produit est requis")]
        [StringLength(255, ErrorMessage = "Le nom ne peut dépasser 255 caractères")]
        [Display(Name = "Nom")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Le prix est requis")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Prix")]
        [Range(0.01, 100000, ErrorMessage = "Le prix doit être entre 0.01 et 100000")]
        public decimal Price { get; set; }

        [Display(Name = "Quantité en stock")]
        [Range(0, int.MaxValue, ErrorMessage = "La quantité ne peut être négative")]
        public int StockQuantity { get; set; }

        [StringLength(100)]
        [Display(Name = "Référence (SKU)")]
        public string SKU { get; set; }

        [Display(Name = "Image")]
        public string ImageUrl { get; set; }

        [Required(ErrorMessage = "La catégorie est requise")]
        [Display(Name = "Catégorie")]
        public int CategoryId { get; set; }

        [Display(Name = "Marque")]
        [StringLength(100)]
        public string Brand { get; set; }

        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Date de modification")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }

        // Calculated properties
        [NotMapped]
        [Display(Name = "Prix avec TVA (20%)")]
        public decimal PriceWithVat => Price * 1.20m;

        [NotMapped]
        [Display(Name = "En stock")]
        public bool InStock => StockQuantity > 0;

        [NotMapped]
        [Display(Name = "Stock faible")]
        public bool LowStock => StockQuantity > 0 && StockQuantity <= 10;
    
}
}
