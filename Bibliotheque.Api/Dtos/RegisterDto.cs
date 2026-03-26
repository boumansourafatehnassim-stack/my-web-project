using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Bibliotheque.Api.Dtos
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = "";

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        public string Prenom { get; set; } = "";

        [Required(ErrorMessage = "Le matricule est obligatoire")]
        [MinLength(5, ErrorMessage = "Le matricule doit contenir au moins 5 caractères")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Le matricule doit contenir uniquement des chiffres")]
        public string Matricule { get; set; } = "";
        public string? Role { get; set; }

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Email invalide (doit contenir @)")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Mot de passe obligatoire")]
        public string MotDePasse { get; set; } = "";

        public IFormFile? Photo { get; set; }
    }
}