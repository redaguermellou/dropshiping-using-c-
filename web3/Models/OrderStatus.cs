using System.ComponentModel.DataAnnotations;

namespace ecom.Enums
{
    public enum OrderStatus
    {
        [Display(Name = "En attente")]
        Pending = 1,

        [Display(Name = "Confirmée")]
        Confirmed = 2,

        [Display(Name = "En préparation")]
        Processing = 3,

        [Display(Name = "Expédiée")]
        Shipped = 4,

        [Display(Name = "Livrée")]
        Delivered = 5,

        [Display(Name = "Annulée")]
        Cancelled = 6,

        [Display(Name = "Remboursée")]
        Refunded = 7
    }

    public enum PaymentStatus
    {
        [Display(Name = "En attente")]
        Pending = 1,

        [Display(Name = "Payé")]
        Paid = 2,

        [Display(Name = "Échoué")]
        Failed = 3,

        [Display(Name = "Remboursé")]
        Refunded = 4
    }

    public enum PaymentMethod
    {
        [Display(Name = "Carte bancaire")]
        CreditCard = 1,

        [Display(Name = "PayPal")]
        PayPal = 2,

        [Display(Name = "Virement bancaire")]
        BankTransfer = 3,

        [Display(Name = "À la livraison")]
        CashOnDelivery = 4
    }

    public enum UserRole
    {
        [Display(Name = "Client")]
        Customer = 1,

        [Display(Name = "Administrateur")]
        Admin = 2,

        [Display(Name = "Gestionnaire")]
        Manager = 3
    }
}