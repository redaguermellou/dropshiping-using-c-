using System.ComponentModel.DataAnnotations;

namespace web3.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Prénom")]
        public string FirstName { get; set; } = "";

        [Display(Name = "Nom")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Display(Name = "Téléphone")]
        public string Phone { get; set; } = "";

        [Display(Name = "Adresse")]
        public string Address { get; set; } = "";

        [Display(Name = "Ville")]
        public string City { get; set; } = "";

        [Display(Name = "Code postal")]
        public string PostalCode { get; set; } = "";

        [Display(Name = "Pays")]
        public string Country { get; set; } = "France";

    }
}
