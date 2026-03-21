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
        public int? UserId { get; set; }

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

        // ✅ Handler واحد فقط للـGET
        public async Task<IActionResult> OnGetAsync()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE")
                return RedirectToPage("/Livres");

            var jwt = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            // إذا ما دخلش UserId نعرض الصفحة فقط
            if (UserId == null || UserId <= 0)
            {
                Emprunts = new();
                return Page();
            }

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var resp = await client.GetAsync($"{apiBase}/api/Emprunts/user/{UserId}/en-cours");
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

        public async Task<IActionResult> OnPostRetourAsync(int empruntId, int userId)
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
                // نرجع نفس الصفحة ونعاود نجبدو emprunts
                UserId = userId;
                return await OnGetAsync();
            }

            Message = "✅ Retour enregistré. Notification envoyée à l'étudiant.";
            return RedirectToPage(new { userId = userId });
        }
    }
}