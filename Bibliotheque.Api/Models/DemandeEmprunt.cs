
namespace Bibliotheque.Api.Models
{
    public class DemandeEmprunt
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ExemplaireId { get; set; }
        public DateTime DateDemande { get; set; }
        public string Statut { get; set; } = "";
        public string? Commentaire { get; set; }
    }
}
