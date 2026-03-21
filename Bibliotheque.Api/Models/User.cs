namespace Bibliotheque.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Nom { get; set; } = null!;
        public string Prenom { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Matricule { get; set; } = null!;

        public string MotDePasseHash { get; set; } = null!;

        public string Role { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateNaissance { get; set; }

        public DateTime? DateCreationCarte { get; set; }

        public DateTime? DateExpirationCarte { get; set; }

        public string? PhotoPath { get; set; }
        public bool CarteImprimee { get; set; } = false;
        public DateTime? DateImpressionCarte { get; set; }
    }
}