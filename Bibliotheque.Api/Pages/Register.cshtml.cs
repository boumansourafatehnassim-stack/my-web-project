using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public RegisterModel(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _env = env;
        }

        [BindProperty] public string Nom { get; set; } = "";
        [BindProperty] public string Prenom { get; set; } = "";
        [BindProperty] public string Matricule { get; set; } = "";
        [BindProperty] public string Role { get; set; } = "ETUDIANT";
        [BindProperty] public string Email { get; set; } = "";
        [BindProperty] public string MotDePasse { get; set; } = "";
        [BindProperty] public IFormFile? Photo { get; set; }

        public string? Message { get; set; }
        public string? Error { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Matricule))
            {
                Error = "Matricule obligatoire.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Role))
            {
                Error = "Role obligatoire.";
                return Page();
            }

            string? photoPath = null;

            if (Photo != null && Photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "users");
                Directory.CreateDirectory(uploadsFolder);

                var extension = Path.GetExtension(Photo.FileName);
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Photo.CopyToAsync(stream);
                }

                photoPath = $"/uploads/users/{fileName}";
            }

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();

            var form = new MultipartFormDataContent
{
    { new StringContent(Nom ?? ""), "Nom" },
    { new StringContent(Prenom ?? ""), "Prenom" },
    { new StringContent(Matricule ?? ""), "Matricule" },
    { new StringContent(Role ?? "ETUDIANT"), "Role" },
    { new StringContent(Email ?? ""), "Email" },
    { new StringContent(MotDePasse ?? ""), "MotDePasse" }
};

            if (Photo != null && Photo.Length > 0)
            {
                var streamContent = new StreamContent(Photo.OpenReadStream());
                streamContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(Photo.ContentType);

                form.Add(streamContent, "Photo", Photo.FileName);
            }

            var resp = await client.PostAsync($"{apiBase}/api/Auth/register", form);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Erreur lors de l'inscription: {body}";
                return Page();
            }

            Message = "Compte créé avec succès. En attente de validation par la bibliothèque.";

            Nom = Prenom = Matricule = Email = MotDePasse = "";
            Role = "ETUDIANT";
            Photo = null;

            return Page();
        }
    }
}