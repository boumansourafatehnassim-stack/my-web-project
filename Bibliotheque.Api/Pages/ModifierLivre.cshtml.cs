using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class ModifierLivreModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ModifierLivreModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private class LivreDto
        {
            public int Id { get; set; }
            public string? Titre { get; set; }
            public string? Auteur { get; set; }
            public string? Theme { get; set; }
            public int? AnneePublication { get; set; }
            public int NombreExemplaires { get; set; }
        }

        [BindProperty] public string Titre { get; set; } = "";
        [BindProperty] public string Auteur { get; set; } = "";
        [BindProperty] public string? Theme { get; set; }
        [BindProperty] public int? AnneePublication { get; set; }
        [BindProperty] public int NombreExemplaires { get; set; } = 1;

        public string? Error { get; set; }
        public string? Success { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                    return RedirectToPage("/Livres");

                var apiBase = $"{Request.Scheme}://{Request.Host}";
                var client = _httpClientFactory.CreateClient();

                var resp = await client.GetAsync($"{apiBase}/api/Livres/{id}");
                if (!resp.IsSuccessStatusCode)
                {
                    Error = "Livre غير موجود.";
                    return Page();
                }

                var livre = await resp.Content.ReadFromJsonAsync<LivreDto>();
                if (livre == null)
                {
                    Error = "Livre غير موجود.";
                    return Page();
                }

                Titre = livre.Titre ?? "";
                Auteur = livre.Auteur ?? "";
                Theme = livre.Theme;
                AnneePublication = livre.AnneePublication;
                NombreExemplaires = livre.NombreExemplaires;

                return Page();
            }
            catch (Exception ex)
            {
                Error = "Erreur chargement: " + ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                    return RedirectToPage("/Livres");

                var jwt = HttpContext.Session.GetString("jwt");
                if (string.IsNullOrEmpty(jwt))
                    return RedirectToPage("/Login");

                if (string.IsNullOrWhiteSpace(Titre) || string.IsNullOrWhiteSpace(Auteur))
                {
                    Error = "Titre و Auteur لازم.";
                    return Page();
                }

                if (NombreExemplaires < 1)
                {
                    Error = "Nombre d'exemplaires لازم يكون 1 أو أكثر.";
                    return Page();
                }

                var apiBase = $"{Request.Scheme}://{Request.Host}";
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

                var resp = await client.PutAsJsonAsync($"{apiBase}/api/Livres/{id}", payload);

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    Error = $"Erreur modification: {(int)resp.StatusCode} - {resp.StatusCode} | {body}";
                    return Page();
                }

                return RedirectToPage("/Livres");
            }
            catch (Exception ex)
            {
                Error = "Erreur modification: " + ex.Message;
                return Page();
            }
        }
    }
}