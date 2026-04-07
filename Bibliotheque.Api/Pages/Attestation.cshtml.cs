using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class AttestationModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public AttestationModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = "";

        public string? Error { get; set; }
        public QuitusResult? Result { get; set; }

        public class QuitusResult
        {
            public int Id { get; set; }
            public string Nom { get; set; } = "";
            public string Prenom { get; set; } = "";
            public string Email { get; set; } = "";
            public string Matricule { get; set; } = "";
            public string Role { get; set; } = "";
            public bool CanGenerate { get; set; }
            public int EmpruntsEnCoursCount { get; set; }
            public string? DocumentText { get; set; }
            public List<EmpruntItem> EmpruntsEnCours { get; set; } = new();
        }

        public class EmpruntItem
        {
            public int Id { get; set; }
            public DateTime DateEmprunt { get; set; }
            public DateTime DateRetourPrevue { get; set; }
            public string LivreTitre { get; set; } = "";
            public string CodeBarres { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Index");

            if (string.IsNullOrWhiteSpace(Search))
                return Page();

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);

                var url = $"{apiBase}/api/Attestations/quitus?search={Uri.EscapeDataString(Search)}";
                var resp = await client.GetAsync(url);
                var json = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Error = json;
                    return Page();
                }

                Result = JsonSerializer.Deserialize<QuitusResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Page();
            }
            catch
            {
                Error = "Impossible de contacter l'API.";
                return Page();
            }
        }
    }
}