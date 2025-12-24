using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ecom.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Numéro de commande")]
        public string OrderNumber { get; set; }

        [Required]
        [Display(Name = "Client")]
        public int UserId { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Montant total")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Statut")]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        [Display(Name = "Adresse de livraison")]
        [DataType(DataType.MultilineText)]
        public string ShippingAddress { get; set; }

        [Display(Name = "Adresse de facturation")]
        [DataType(DataType.MultilineText)]
        public string BillingAddress { get; set; }

        [Display(Name = "Méthode de paiement")]
        [StringLength(50)]
        public string PaymentMethod { get; set; }

        [Display(Name = "Statut du paiement")]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "Date de commande")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Date de modification")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Date d'expédition")]
        public DateTime? ShippedAt { get; set; }

        [Display(Name = "Date de livraison")]
        public DateTime? DeliveredAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }

        // Calculated properties
        [NotMapped]
        [Display(Name = "Nombre d'articles")]
        public int ItemCount => OrderDetails?.Sum(od => od.Quantity) ?? 0;

        [NotMapped]
        public bool IsPending => Status == "Pending";

        [NotMapped]
        public bool IsPaid => PaymentStatus == "Paid";

        [NotMapped]
        public bool IsShipped => !string.IsNullOrEmpty(Status) &&
                                 (Status == "Shipped" || Status == "Delivered");

        [NotMapped]
        [Display(Name = "Date formatée")]
        public string FormattedDate => CreatedAt.ToString("dd/MM/yyyy HH:mm");
    }
}
