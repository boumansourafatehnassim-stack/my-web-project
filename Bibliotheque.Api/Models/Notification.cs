namespace Bibliotheque.Api.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = null!;
        public DateTime DateCreation { get; set; }
        public bool Lu { get; set; }
        public string Type { get; set; } = null!;
        public int? SujetDemandeId { get; set; }
        public int? SujetEmpruntId { get; set; }
    }
}
