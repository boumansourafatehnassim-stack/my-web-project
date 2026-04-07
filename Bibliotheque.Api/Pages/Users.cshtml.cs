using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Api.Pages
{
    public class UsersModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public UsersModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public List<UserItem> Items { get; set; } = new();
        public string? Error { get; set; }
        public string? Message { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = "";

        [BindProperty]
        public int UserId { get; set; }

        [BindProperty]
        public string NewPassword { get; set; } = "";

        public class UserItem
        {
            public int Id { get; set; }
            public string Nom { get; set; } = "";
            public string Prenom { get; set; } = "";
            public string Email { get; set; } = "";
            public string Matricule { get; set; } = "";
            public string Role { get; set; } = "";
            public bool IsActive { get; set; }
            public DateTime DateCreation { get; set; }
            public string? PhotoPath { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Index");

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);

                var resp = await client.GetAsync($"{apiBase}/api/Users");

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    Error = $"Erreur chargement utilisateurs: {body}";
                    return Page();
                }

                var json = await resp.Content.ReadAsStringAsync();

                var allItems = JsonSerializer.Deserialize<List<UserItem>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();

                if (!string.IsNullOrWhiteSpace(Search))
                {
                    var term = Search.Trim().ToLower();

                    allItems = allItems
                        .Where(u =>
                            (!string.IsNullOrWhiteSpace(u.Email) && u.Email.ToLower().Contains(term)) ||
                            (!string.IsNullOrWhiteSpace(u.Matricule) && u.Matricule.ToLower().Contains(term))
                        )
                        .ToList();
                }

                Items = allItems;
                return Page();
            }
            catch
            {
                Error = "Impossible de contacter l'API.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleActiveAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Index");

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);

                var resp = await client.PutAsync(
                    $"{apiBase}/api/Users/{UserId}/toggle-active",
                    new StringContent("", Encoding.UTF8, "application/json")
                );

                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Error = $"Erreur changement état: {body}";
                    return await OnGetAsync();
                }

                Message = "État du compte modifié avec succès ✅";
                return RedirectToPage(new { Search });
            }
            catch
            {
                Error = "Impossible de contacter l'API.";
                return await OnGetAsync();
            }
        }

        public async Task<IActionResult> OnPostResetPasswordAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Index");

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                Error = "Le nouveau mot de passe est obligatoire.";
                return await OnGetAsync();
            }

            try
            {
                var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);

                var payload = new
                {
                    newPassword = NewPassword
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var resp = await client.PutAsync(
                    $"{apiBase}/api/Users/{UserId}/reset-password",
                    content
                );

                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Error = $"Erreur changement mot de passe: {body}";
                    return await OnGetAsync();
                }

                Message = "Mot de passe modifié avec succès ✅";
                NewPassword = "";
                return RedirectToPage(new { Search });
            }
            catch
            {
                Error = "Impossible de contacter l'API.";
                return await OnGetAsync();
            }
        }
    }
}