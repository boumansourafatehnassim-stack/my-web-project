using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Bibliotheque.Api.Pages
{
    public class LivresCreateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public LivresCreateModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty] public string Titre { get; set; } = "";
        [BindProperty] public string Auteur { get; set; } = "";
        [BindProperty] public string? Theme { get; set; }
        [BindProperty] public int? AnneePublication { get; set; }
        [BindProperty] public int NombreExemplaires { get; set; } = 1;

        public string? Message { get; set; }
        public string? Error { get; set; }

        public IActionResult OnGet()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Livres");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Livres");

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var payload = new
            {
                titre = Titre,
                auteur = Auteur,
                theme = Theme,
                anneePublication = AnneePublication,
                nombreExemplaires = NombreExemplaires
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var resp = await client.PostAsync($"{apiBase}/api/Livres", content);

            if (!resp.IsSuccessStatusCode)
            {
                Error = "Erreur lors de l'ajout du livre.";
                return Page();
            }

            return RedirectToPage("/Livres");
        }
    }
}