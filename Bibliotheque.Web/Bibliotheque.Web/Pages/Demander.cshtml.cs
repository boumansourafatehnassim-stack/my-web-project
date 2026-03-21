using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Web.Pages
{
    public class DemanderModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public DemanderModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int LivreId { get; set; }

        [BindProperty]
        public string? Commentaire { get; set; }

        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime DateDemande { get; set; }
        public DateTime DateRetourPrevue { get; set; }

        public string? Error { get; set; }
        public string? Message { get; set; }

        public IActionResult OnGet()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var userId = HttpContext.Session.GetInt32("userId");

            if (string.IsNullOrEmpty(jwt) || userId == null)
                return RedirectToPage("/Login");

            Email = HttpContext.Session.GetString("email") ?? "";
            Role = HttpContext.Session.GetString("role") ?? "";

            DateDemande = DateTime.Today;
            DateRetourPrevue = DateTime.Today.AddDays(14); // مدة إعارة افتراضية (14 يوم)

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var userId = HttpContext.Session.GetInt32("userId");

            if (string.IsNullOrEmpty(jwt) || userId == null)
                return RedirectToPage("/Login");

            Email = HttpContext.Session.GetString("email") ?? "";
            Role = HttpContext.Session.GetString("role") ?? "";
            DateDemande = DateTime.Today;
            DateRetourPrevue = DateTime.Today.AddDays(14);

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);

                var payload = new
                {
                    userId = userId.Value,
                    livreId = LivreId,
                    commentaire = Commentaire
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var resp = await client.PostAsync($"{apiBase}/api/Demandes", content);
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Error = $"Erreur API: {body}";
                    return Page();
                }

                Message = "✅ Demande envoyée. Statut: EN_ATTENTE (en attente de validation).";
                Commentaire = "";
                return Page();
            }
            catch (Exception ex)
            {
                Error = "Erreur: " + ex.Message;
                return Page();
            }
        }
    }
}
