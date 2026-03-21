using Microsoft.AspNetCore.Http;

namespace Bibliotheque.Api.Dtos
{
    public class RegisterDto
    {
        public string Nom { get; set; } = "";
        public string Prenom { get; set; } = "";
        public string Matricule { get; set; } = "";
        public string? Role { get; set; }
        public string Email { get; set; } = "";
        public string MotDePasse { get; set; } = "";
        public IFormFile? Photo { get; set; }
    }
}