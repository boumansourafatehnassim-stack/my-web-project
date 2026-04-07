using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class InscriptionsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public InscriptionsModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public List<InscriptionItem> Items { get; set; } = new();
        public string? Error { get; set; }
        public string? Message { get; set; }

        [BindProperty] public int DemandeId { get; set; }
        [BindProperty] public string Action { get; set; } = "";
        [BindProperty] public bool CreerCarte { get; set; }

        public class InscriptionItem
        {
            public int Id { get; set; }
            public int UserId { get; set; }

            public string Nom { get; set; } = "";
            public string Prenom { get; set; } = "";
            public string Email { get; set; } = "";
            public string Matricule { get; set; } = "";
            public string? PhotoPath { get; set; }

            public DateTime DateDemande { get; set; }
            public string Statut { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var resp = await client.GetAsync($"{apiBase}/api/InscriptionDemandes?statut=EN_ATTENTE");
                if (!resp.IsSuccessStatusCode)
                {
                    Error = "Erreur أثناء جلب demandes d'inscription.";
                    return Page();
                }

                var json = await resp.Content.ReadAsStringAsync();
                Items = JsonSerializer.Deserialize<List<InscriptionItem>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();

                return Page();
            }
            catch
            {
                Error = "Impossible de contacter l'API.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var payload = new { demandeId = DemandeId, action = Action, creerCarte = CreerCarte };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var resp = await client.PostAsync($"{apiBase}/api/InscriptionDemandes/traiter", content);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    Error = $"Erreur traitement: {body}";
                    return await OnGetAsync();
                }

                Message = "Traitement OK ✅";
                return await OnGetAsync();
            }
            catch
            {
                Error = "Impossible de contacter l'API.";
                return await OnGetAsync();
            }
        }
    }
}
