public class Exemplaire
{
    public int Id { get; set; }
    public int LivreId { get; set; }
    public string CodeBarres { get; set; } = "";
    public string Statut { get; set; } = "DISPONIBLE";
    public string? Emplacement { get; set; }
}