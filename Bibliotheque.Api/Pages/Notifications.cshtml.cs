using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class NotificationsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public NotificationsModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public string? Error { get; set; }
        public List<NotificationDto> Notifications { get; set; } = new();

        public class NotificationDto
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string Message { get; set; } = "";
            public DateTime DateCreation { get; set; }
            public bool Lu { get; set; }
            public string Type { get; set; } = "";
            public int? SujetDemandeId { get; set; }
            public int? SujetEmpruntId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var userId = HttpContext.Session.GetInt32("userId");

            if (string.IsNullOrEmpty(jwt) || userId == null)
                return RedirectToPage("/Login");

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var resp = await client.GetAsync($"{apiBase}/api/Notifications/user/{userId.Value}");
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Error = $"Erreur API: {resp.StatusCode} | {body}";
                    Notifications = new();
                    return Page();
                }

                Notifications = JsonSerializer.Deserialize<List<NotificationDto>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                // ترتيب من الأحدث للأقدم (اختياري)
                Notifications = Notifications
                    .OrderByDescending(x => x.DateCreation)
                    .ToList();

                return Page();
            }
            catch (Exception ex)
            {
                Error = "Erreur: " + ex.Message;
                Notifications = new();
                return Page();
            }
        }

        // ✅ بدل PATCH نخدم POST (توافق 100%)
        public async Task<IActionResult> OnPostLuAsync(int id)
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var userId = HttpContext.Session.GetInt32("userId");

            if (string.IsNullOrEmpty(jwt) || userId == null)
                return RedirectToPage("/Login");

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            // نحاول 3 طرق باش نضمن النجاح حسب API عندك:
            // 1) POST /api/Notifications/{id}/lu
            var resp = await client.PostAsync($"{apiBase}/api/Notifications/{id}/lu", new StringContent("{}", Encoding.UTF8, "application/json"));

            if (!resp.IsSuccessStatusCode)
            {
                // 2) PUT /api/Notifications/{id}/lu
                resp = await client.PutAsync($"{apiBase}/api/Notifications/{id}/lu", new StringContent("{}", Encoding.UTF8, "application/json"));
            }

            if (!resp.IsSuccessStatusCode)
            {
                // 3) PATCH fallback يدوي إذا API يفرض PATCH
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{apiBase}/api/Notifications/{id}/lu");
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                resp = await client.SendAsync(request);
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Erreur API: {resp.StatusCode} | {body}";
                return await OnGetAsync(); // نبقى في نفس الصفحة ونظهر الخطأ
            }

            return RedirectToPage(new { }); // refresh
        }
    }
}