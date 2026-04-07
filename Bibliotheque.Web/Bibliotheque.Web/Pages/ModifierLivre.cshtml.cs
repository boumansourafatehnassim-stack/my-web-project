using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class ModifierLivreModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ModifierLivreModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // DTO محلي في Web (باش ما نحتاجوش Reference للـApi)
        private class LivreDto
        {
            public int Id { get; set; }
            public string? Titre { get; set; }
            public string? Auteur { get; set; }
            public string? Theme { get; set; }
            public int? AnneePublication { get; set; }
        }

        [BindProperty] public string Titre { get; set; } = "";
        [BindProperty] public string Auteur { get; set; } = "";
        [BindProperty] public string? Theme { get; set; }
        [BindProperty] public int? AnneePublication { get; set; }

        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // حماية: غير bibliothécaire
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE")
                return RedirectToPage("/Livres");

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();

            var livre = await client.GetFromJsonAsync<LivreDto>($"{apiBase}/api/Livres/{id}");
            if (livre == null)
            {
                Error = "Livre غير موجود.";
                return Page();
            }

            Titre = livre.Titre ?? "";
            Auteur = livre.Auteur ?? "";
            Theme = livre.Theme;
            AnneePublication = livre.AnneePublication;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
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

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var payload = new
            {
                titre = Titre,
                auteur = Auteur,
                theme = Theme,
                anneePublication = AnneePublication
            };

            var resp = await client.PutAsJsonAsync($"{apiBase}/api/Livres/{id}", payload);
            if (!resp.IsSuccessStatusCode)
            {
                Error = await resp.Content.ReadAsStringAsync();
                return Page();
            }

            return RedirectToPage("/Livres");
        }
    }
}