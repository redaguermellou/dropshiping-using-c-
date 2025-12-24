using System.ComponentModel.DataAnnotations;

namespace web3.ViewModels
{
    public class LogingViewModel
    {
        [Required(ErrorMessage = "L'email ou nom d'utilisateur est requis")]
        [Display(Name = "Email ou Nom d'utilisateur")]
        public string EmailOrUsername { get; set; } = "";

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = "";

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }

    }
}
