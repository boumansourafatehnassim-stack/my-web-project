using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class DemandesModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public DemandesModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public List<DemandeVm> Demandes { get; set; } = new();
        public string? Error { get; set; }
        public string? Info { get; set; }

        public class DemandeVm
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public int? ExemplaireId { get; set; }

            public string Nom { get; set; } = "";
            public string Prenom { get; set; } = "";
            public string Email { get; set; } = "";
            public string Matricule { get; set; } = "";
            public string? PhotoPath { get; set; }

            public string LivreTitre { get; set; } = "";

            public DateTime DateDemande { get; set; }
            public string Statut { get; set; } = "";
            public string? Commentaire { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
            {
                Error = null;
                return Page();
            }

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            HttpResponseMessage resp;

            try
            {
                resp = await client.GetAsync($"{apiBase}/api/Demandes");
            }
            catch
            {
                Error = "تعذر الاتصال بالـAPI. تأكد Bibliotheque.Api راهو خدام.";
                return Page();
            }

            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Error = $"Erreur API: {resp.StatusCode}";
                return Page();
            }

            try
            {
                Demandes = JsonSerializer.Deserialize<List<DemandeVm>>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new();
            }
            catch
            {
                Error = "فشل parsing للنتيجة (JSON).";
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostTraiterAsync(int demandeId, string action)
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
            {
                Error = "ما عندكش صلاحية.";
                return await OnGetAsync();
            }

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var payload = new { demandeId, action };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage resp;

            try
            {
                resp = await client.PostAsync($"{apiBase}/api/Demandes/traiter", content);
            }
            catch
            {
                Error = "تعذر الاتصال بالـAPI.";
                return await OnGetAsync();
            }

            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Error = "فشل معالجة الطلب.";

                try
                {
                    using var errDoc = JsonDocument.Parse(body);

                    var errorText = "";
                    var innerText = "";

                    if (errDoc.RootElement.TryGetProperty("error", out var e))
                        errorText = e.GetString() ?? "";

                    if (errDoc.RootElement.TryGetProperty("inner", out var i))
                        innerText = i.GetString() ?? "";

                    Error = string.IsNullOrWhiteSpace(innerText)
                        ? errorText
                        : $"{errorText} | INNER: {innerText}";
                }
                catch
                {
                    Error = body;
                }

                return await OnGetAsync();
            }

            Info = $"تمت العملية: {action} للطلب #{demandeId}";
            return await OnGetAsync();
        }
    }
}