namespace Bibliotheque.Api.Models
{
    public class Emprunt
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ExemplaireId { get; set; }

        public DateTime DateEmprunt { get; set; }

        public DateTime DateRetourPrevue { get; set; }

        public DateTime? DateRetourReelle { get; set; }

        // حالة الإعارة
        public string Statut { get; set; } = "EN_COURS";
    }
}