using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Web.Pages
{
    public class LivresModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public LivresModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public List<LivreDto> Livres { get; set; } = new();
        public string? Error { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Theme { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 1000;

        public int Total { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();

                var jwt = HttpContext.Session.GetString("jwt");
                if (!string.IsNullOrEmpty(jwt))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var url = $"{apiBase}/api/Livres?page={Page}&pageSize={PageSize}";

                if (!string.IsNullOrWhiteSpace(Search))
                    url += $"&search={Uri.EscapeDataString(Search.Trim())}";

                if (!string.IsNullOrWhiteSpace(Theme))
                    url += $"&theme={Uri.EscapeDataString(Theme.Trim())}";

                var resp = await client.GetAsync(url);
                var json = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Error = $"Erreur API: {resp.StatusCode} | {json}";
                    return Page();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                using var doc = JsonDocument.Parse(json);

                // ✅ إذا API يرجع Object فيه items
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("items", out var itemsEl) ||
                        doc.RootElement.TryGetProperty("Items", out itemsEl))
                    {
                        Livres = JsonSerializer.Deserialize<List<LivreDto>>(itemsEl.GetRawText(), options) ?? new();

                        if (doc.RootElement.TryGetProperty("total", out var totalEl) ||
                            doc.RootElement.TryGetProperty("Total", out totalEl))
                        {
                            Total = totalEl.GetInt32();
                        }

                        return Page();
                    }
                }

                // ✅ إذا API يرجع Array مباشرة
                Livres = JsonSerializer.Deserialize<List<LivreDto>>(json, options) ?? new();
                Total = Livres.Count;

                return Page();
            }
            catch (Exception ex)
            {
                Error = "Erreur: " + ex.Message;
                return Page();
            }
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var role = HttpContext.Session.GetString("role");
                if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                    return RedirectToPage("/Livres");

                var jwt = HttpContext.Session.GetString("jwt");
                if (string.IsNullOrEmpty(jwt))
                    return RedirectToPage("/Login");

                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var resp = await client.DeleteAsync($"{apiBase}/api/Livres/{id}");

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    Error = $"Erreur suppression: {resp.StatusCode} | {body}";
                    await OnGetAsync();
                    return Page();
                }

                return RedirectToPage("/Livres");
            }
            catch (Exception ex)
            {
                Error = "Erreur suppression: " + ex.Message;
                await OnGetAsync();
                return Page();
            }
        }

        public class LivreDto
        
        {
            public int Id { get; set; }
            public string Titre { get; set; } = "";
            public string Auteur { get; set; } = "";
            public string? Theme { get; set; }
            public int? AnneePublication { get; set; }

            public string? Langue { get; set; }
            public string? AdresseBibliogr { get; set; }

            public bool IsDeleted { get; set; }
            public int NombreExemplaires { get; set; }
            public int NombreDisponibles { get; set; }
        }
    }
}