using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Bibliotheque.Web.Pages
{
    public class LivresEditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public LivresEditModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty] public int Id { get; set; }
        [BindProperty] public string Titre { get; set; } = "";
        [BindProperty] public string Auteur { get; set; } = "";
        [BindProperty] public string? Theme { get; set; }
        [BindProperty] public int? AnneePublication { get; set; }

        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Livres");

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();

            var resp = await client.GetAsync($"{apiBase}/api/Livres/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                Error = "Livre introuvable.";
                return Page();
            }

            var json = await resp.Content.ReadAsStringAsync();
            var livre = JsonSerializer.Deserialize<LivreEditDto>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (livre == null)
            {
                Error = "Livre introuvable.";
                return Page();
            }

            Id = livre.Id;
            Titre = livre.Titre;
            Auteur = livre.Auteur;
            Theme = livre.Theme;
            AnneePublication = livre.AnneePublication;

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
                id = Id,
                titre = Titre,
                auteur = Auteur,
                theme = Theme,
                anneePublication = AnneePublication
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var resp = await client.PutAsync($"{apiBase}/api/Livres/{Id}", content);

            if (!resp.IsSuccessStatusCode)
            {
                Error = "Erreur lors de la modification du livre.";
                return Page();
            }

            return RedirectToPage("/Livres");
        }

        public class LivreEditDto
        {
            public int Id { get; set; }
            public string Titre { get; set; } = "";
            public string Auteur { get; set; } = "";
            public string? Theme { get; set; }
            public int? AnneePublication { get; set; }
        }
    }
}