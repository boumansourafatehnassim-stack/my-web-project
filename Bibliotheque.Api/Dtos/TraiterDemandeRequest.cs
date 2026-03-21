namespace Bibliotheque.Api.Dtos
{
    public class TraiterDemandeRequest
    {
        public int DemandeId { get; set; }
        public string Action { get; set; } = null!; // "VALIDER" ou "REFUSER"
        public bool CreerCarte { get; set; }
    }
}