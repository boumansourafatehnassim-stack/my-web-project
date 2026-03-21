using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Models
{
    [Keyless]
    public class TraiterDemandeResult
    {
        public string Statut { get; set; } = null!;
        public int? EmpruntId { get; set; } // يجي غير في حالة VALIDER
    }
}
