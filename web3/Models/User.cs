using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ecom.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [StringLength(100, ErrorMessage = "Le nom d'utilisateur ne peut dépasser 100 caractères")]
        [Display(Name = "Nom d'utilisateur")]
        public string Username { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(255)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Mot de passe hashé")]
        public string PasswordHash { get; set; }

        [StringLength(100)]
        [Display(Name = "Prénom")]
        public string FirstName { get; set; }

        [StringLength(100)]
        [Display(Name = "Nom")]
        public string LastName { get; set; }

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20)]
        [Display(Name = "Téléphone")]
        public string Phone { get; set; }

        [Display(Name = "Adresse")]
        [DataType(DataType.MultilineText)]
        public string Address { get; set; }

        [Display(Name = "Ville")]
        [StringLength(100)]
        public string City { get; set; }

        [Display(Name = "Code postal")]
        [StringLength(10)]
        public string PostalCode { get; set; }

        [Display(Name = "Pays")]
        [StringLength(100)]
        public string Country { get; set; } = "France";

        [Display(Name = "Rôle")]
        [StringLength(50)]
        public string Role { get; set; } = "Customer";

        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Email vérifié")]
        public bool EmailVerified { get; set; } = false;

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Date de modification")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Dernière connexion")]
        public DateTime? LastLogin { get; set; }

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; }
        public virtual Cart Cart { get; set; }

        // Calculated properties
        [NotMapped]
        [Display(Name = "Nom complet")]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [NotMapped]
        public bool IsAdmin => Role == "Admin";

        [NotMapped]
        public string Initials =>
            !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName)
                ? $"{FirstName[0]}{LastName[0]}".ToUpper()
                : Username.Substring(0, Math.Min(2, Username.Length)).ToUpper();
    }
}
