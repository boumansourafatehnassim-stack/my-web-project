using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public DashboardStats Stats { get; set; } = new();

        public class DashboardStats
        {
            public int TotalUsers { get; set; }
            public int ActiveUsers { get; set; }
            public int TotalLivres { get; set; }
            public int TotalDemandes { get; set; }
            public int DemandesEnAttente { get; set; }
            public int TotalEmprunts { get; set; }
            public int EmpruntsEnCours { get; set; }
            public int TotalNotifications { get; set; }
        }

        public async Task OnGetAsync()
        {
            try
            {
                var jwt = HttpContext.Session.GetString("jwt");
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');

                var client = _httpClientFactory.CreateClient();

                if (!string.IsNullOrEmpty(jwt))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", jwt);
                }

                var resp = await client.GetAsync($"{apiBase}/api/dashboard/stats");

                if (!resp.IsSuccessStatusCode)
                    return;

                var json = await resp.Content.ReadAsStringAsync();

                Stats = JsonSerializer.Deserialize<DashboardStats>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new DashboardStats();
            }
            catch
            {
            }
        }
    }
}