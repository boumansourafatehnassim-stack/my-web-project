namespace Bibliotheque.Api.Models
{
    public class DemandeInscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime DateDemande { get; set; }
        public string Statut { get; set; } = "EN_ATTENTE";
        public string? Commentaire { get; set; }
    }
}
