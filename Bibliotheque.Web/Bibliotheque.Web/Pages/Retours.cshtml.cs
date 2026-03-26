using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Web.Pages
{
    public class RetoursModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public RetoursModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public string? Matricule { get; set; }

        public string? Error { get; set; }
        public string? Message { get; set; }

        public List<EmpruntDto> Emprunts { get; set; } = new();

        public class EmpruntDto
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int ExemplaireId { get; set; }
            public DateTime DateEmprunt { get; set; }
            public DateTime? DateRetourPrevue { get; set; }
            public DateTime? DateRetourReelle { get; set; }
        }

        public class UserLookupDto
        {
            public int Id { get; set; }
            public string Nom { get; set; } = "";
            public string Prenom { get; set; } = "";
            public string? Matricule { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE")
                return RedirectToPage("/Livres");

            var jwt = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (string.IsNullOrWhiteSpace(Matricule))
            {
                Emprunts = new();
                return Page();
            }

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            // 1) نلقاو user انطلاقًا من matricule
            var userResp = await client.GetAsync($"{apiBase}/api/Users/by-matricule/{Uri.EscapeDataString(Matricule.Trim())}");
            var userBody = await userResp.Content.ReadAsStringAsync();

            if (!userResp.IsSuccessStatusCode)
            {
                Error = "Aucun étudiant trouvé avec ce matricule.";
                Emprunts = new();
                return Page();
            }

            var user = JsonSerializer.Deserialize<UserLookupDto>(userBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (user == null || user.Id <= 0)
            {
                Error = "Aucun étudiant trouvé avec ce matricule.";
                Emprunts = new();
                return Page();
            }

            // 2) نجيبو emprunts en cours تاعو بالـ userId
            var resp = await client.GetAsync($"{apiBase}/api/Emprunts/user/{user.Id}/en-cours");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Error = $"Erreur API: {body}";
                Emprunts = new();
                return Page();
            }

            Emprunts = JsonSerializer.Deserialize<List<EmpruntDto>>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            return Page();
        }

        public async Task<IActionResult> OnPostRetourAsync(int empruntId, string matricule)
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE")
                return RedirectToPage("/Livres");

            var jwt = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var payload = new { empruntId = empruntId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await client.PostAsync($"{apiBase}/api/Emprunts/retour", content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Error = $"Erreur retour: {body}";
                Matricule = matricule;
                return await OnGetAsync();
            }

            Message = "✅ Retour enregistré. Notification envoyée à l'étudiant.";
            return RedirectToPage(new { matricule = matricule });
        }
    }
}