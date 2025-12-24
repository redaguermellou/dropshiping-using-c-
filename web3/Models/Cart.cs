using ecom.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client")]
        public int UserId { get; set; }

        [Display(Name = "Code de session")]
        [StringLength(100)]
        public string SessionId { get; set; }

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Date de modification")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();

        // Calculated properties
        [NotMapped]
        [Display(Name = "Total")]
        public decimal TotalAmount => Items.Sum(item => item.Subtotal);

        [NotMapped]
        [Display(Name = "Total avec TVA")]
        public decimal TotalAmountWithVat => TotalAmount * 1.20m;

        [NotMapped]
        [Display(Name = "Nombre d'articles")]
        public int ItemCount => Items.Sum(item => item.Quantity);

        [NotMapped]
        public bool IsEmpty => !Items.Any();

        // Methods
        public void AddItem(Product product, int quantity)
        {
            var existingItem = Items.FirstOrDefault(item => item.ProductId == product.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    AddedAt = DateTime.UtcNow
                });
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveItem(int productId)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                Items.Remove(item);
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void Clear()
        {
            Items.Clear();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}