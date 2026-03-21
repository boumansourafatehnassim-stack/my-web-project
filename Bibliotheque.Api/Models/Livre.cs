namespace Bibliotheque.Api.Models
{
    public class Livre
    {
        public int Id { get; set; }
        public string Titre { get; set; } = null!;
        public string Auteur { get; set; } = null!;
        public string? Theme { get; set; }
        public int? AnneePublication { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? Langue { get; set; }
        public string? AdresseBibliogr { get; set; }
    }
}
