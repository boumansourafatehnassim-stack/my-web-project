using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Web.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public LoginModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty] public string Email { get; set; } = "";
        [BindProperty] public string MotDePasse { get; set; } = "";

        public string? Error { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();

            var payload = new { email = Email, motDePasse = MotDePasse };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage resp;
            try
            {
                resp = await client.PostAsync($"{apiBase}/api/Auth/login", content);
            }
            catch
            {
                Error = "تعذر الاتصال بالـAPI. تأكد أن Bibliotheque.Api راهو خدام.";
                return Page();
            }

            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                try
                {
                    using var errDoc = JsonDocument.Parse(body);
                    if (errDoc.RootElement.TryGetProperty("error", out var e))
                        Error = e.GetString();
                    else
                        Error = "Email أو كلمة السر خاطئة";
                }
                catch
                {
                    Error = "Email أو كلمة السر خاطئة";
                }
                return Page();
            }

            using var doc = JsonDocument.Parse(body);

            var token = doc.RootElement.GetProperty("token").GetString();
            if (string.IsNullOrEmpty(token))
            {
                Error = "Token ما رجعش من السيرفر";
                return Page();
            }

            // ✅ نخزن jwt مباشرة
            HttpContext.Session.SetString("jwt", token);

            // ✅ نجيب user infos (id/role/email)
            if (doc.RootElement.TryGetProperty("user", out var userEl))
            {
                if (userEl.TryGetProperty("id", out var idEl))
                    HttpContext.Session.SetInt32("userId", idEl.GetInt32());

                if (userEl.TryGetProperty("role", out var r))
                    HttpContext.Session.SetString("role", r.GetString() ?? "");

                if (userEl.TryGetProperty("email", out var em))
                    HttpContext.Session.SetString("email", em.GetString() ?? "");
            }
            else
            {
                // إذا الـAPI ما رجّعش user object
                Error = "السيرفر رجع token لكن ما رجعش user.";
                return Page();
            }

            return RedirectToPage("/Index");
        }
    }
}
