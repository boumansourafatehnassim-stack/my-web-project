namespace Bibliotheque.Api.Dtos
{
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string MotDePasse { get; set; } = null!;
    }
}
