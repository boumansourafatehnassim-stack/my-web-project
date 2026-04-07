using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class CarteEtudiantModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public CarteEtudiantModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public CarteDto? Carte { get; set; }
        public string? Error { get; set; }

        public class CarteDto
        {
            public int Id { get; set; }
            public string Nom { get; set; } = "";
            public string Prenom { get; set; } = "";
            public string Email { get; set; } = "";
            public string Matricule { get; set; } = "";
            
            public string? PhotoPath { get; set; }
            public DateTime? DateNaissance { get; set; }
            public DateTime? DateCreationCarte { get; set; }
            public DateTime? DateExpirationCarte { get; set; }
            public bool IsActive { get; set; }
            public string Role { get; set; } = "";
            public bool CarteImprimee { get; set; }
            public DateTime? DateImpressionCarte { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var userId = HttpContext.Session.GetInt32("userId");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            int targetId;

            if (id.HasValue)
            {
                if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                    return RedirectToPage("/Index");

                targetId = id.Value;
            }
            else
            {
                if (!userId.HasValue)
                    return RedirectToPage("/Login");

                targetId = userId.Value;
            }

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);

                var resp = await client.GetAsync($"{apiBase}/api/Users/{targetId}/carte");
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    Error = $"Impossible de charger la carte étudiant. {body}";
                    return Page();
                }
                var json = await resp.Content.ReadAsStringAsync();
                Carte = JsonSerializer.Deserialize<CarteDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (Carte == null)
                    Error = "Carte introuvable.";

                return Page();
            }
            catch
            {
                Error = "Impossible de contacter l'API.";
                return Page();
            }
        }
        public async Task<IActionResult> OnPostMarquerImpressionAsync(int id)
        {
            var jwt = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(jwt))
                return Unauthorized();

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);

                var resp = await client.PutAsync($"{apiBase}/api/Users/{id}/marquer-impression-carte", null);

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    return Content(body);
                }

                return Content("OK");
            }
            catch
            {
                return Content("Erreur serveur");
            }
        }
    }
}