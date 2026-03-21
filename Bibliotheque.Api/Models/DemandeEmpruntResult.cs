using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Models
{
    [Keyless]
    public class DemandeEmpruntResult
    {
        public int DemandeId { get; set; }
        public int? ExemplaireId { get; set; }
        public string Statut { get; set; } = null!;
    }
}
