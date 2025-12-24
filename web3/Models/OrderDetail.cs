using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ecom.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Commande")]
        public int OrderId { get; set; }

        [Required]
        [Display(Name = "Produit")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Quantité")]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être au moins 1")]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Prix unitaire")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Date d'ajout")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // Calculated properties
        [NotMapped]
        [Display(Name = "Sous-total")]
        public decimal Subtotal => UnitPrice * Quantity;

        [NotMapped]
        [Display(Name = "Sous-total avec TVA")]
        public decimal SubtotalWithVat => Subtotal * 1.20m;
    }
}
