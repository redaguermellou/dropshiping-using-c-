using ecom.Models;

using System.ComponentModel.DataAnnotations;

namespace ecom.ViewModels
{
    public class CartViewModel
    {
        public Cart Cart { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountWithVat { get; set; }
    }

    public class CheckoutViewModel
    {
        public Cart Cart { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le téléphone est requis")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "L'adresse est requise")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "La ville est requise")]
        public string City { get; set; }

        [Required(ErrorMessage = "Le code postal est requis")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Le pays est requis")]
        public string Country { get; set; }

        [Required(ErrorMessage = "La méthode de paiement est requise")]
        public string PaymentMethod { get; set; }

        public string Notes { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal TotalAmountWithVat { get; set; }
    }

    public class CartSummaryViewModel
    {
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}