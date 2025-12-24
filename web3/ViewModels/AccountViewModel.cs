using System.ComponentModel.DataAnnotations;

namespace ecom.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "L'email ou nom d'utilisateur est requis")]
        public string EmailOrUsername { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Email invalide")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Numéro de téléphone invalide")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Minimum 8 caractères")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; }
    }

    public class ProfileViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }
}