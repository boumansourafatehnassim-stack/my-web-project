using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bibliotheque.Api.Models
{
    public class Livre
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Titre { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Auteur { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Theme { get; set; }

        public int? AnneePublication { get; set; }

        [MaxLength(100)]
        public string? Langue { get; set; }

        [MaxLength(1000)]
        public string? AdresseBibliogr { get; set; }

        public bool IsDeleted { get; set; } = false;

        public List<Exemplaire> Exemplaires { get; set; } = new();
    }
}