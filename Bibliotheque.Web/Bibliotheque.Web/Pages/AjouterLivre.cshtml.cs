using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class AjouterLivreModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public AjouterLivreModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty] public string Titre { get; set; } = "";
        [BindProperty] public string Auteur { get; set; } = "";
        [BindProperty] public string? Theme { get; set; }
        [BindProperty] public int? AnneePublication { get; set; }
        [BindProperty] public int? NombreExemplaires { get; set; }

        public string? Error { get; set; }

        public IActionResult OnGet()
        {
            // حماية: ما يدخلش إلا Bibliothécaire
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE")
                return RedirectToPage("/Livres");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE")
                return RedirectToPage("/Livres");

            var jwt = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (string.IsNullOrWhiteSpace(Titre) || string.IsNullOrWhiteSpace(Auteur))
            {
                Error = "Titre و Auteur لازم.";
                return Page();
            }

            if (!NombreExemplaires.HasValue || NombreExemplaires.Value < 1)
            {
                Error = "Nombre d'exemplaires لازم يكون 1 أو أكثر.";
                return Page();
            }

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

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
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"فشل إضافة الكتاب. Status: {(int)resp.StatusCode} - {resp.StatusCode} | Body: {body}";
                return Page();
            }

            return RedirectToPage("/Livres");
        }
    }
}