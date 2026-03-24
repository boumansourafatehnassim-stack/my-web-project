using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bibliotheque.Api.Models
{
    public class Exemplaire
    {
        public int Id { get; set; }

        [Required]
        public int LivreId { get; set; }

        [ForeignKey(nameof(LivreId))]
        public Livre Livre { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string CodeBarres { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Statut { get; set; } = "DISPONIBLE";

        [MaxLength(200)]
        public string? Emplacement { get; set; }
    }
}