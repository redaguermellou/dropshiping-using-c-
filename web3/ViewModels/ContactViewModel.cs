using System.ComponentModel.DataAnnotations;

namespace web3.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [Display(Name = "Nom")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Le sujet est requis")]
        [Display(Name = "Sujet")]
        public string Subject { get; set; } = "";

        [Required(ErrorMessage = "Le message est requis")]
        [Display(Name = "Message")]
        public string Message { get; set; } = "";
    }
}
