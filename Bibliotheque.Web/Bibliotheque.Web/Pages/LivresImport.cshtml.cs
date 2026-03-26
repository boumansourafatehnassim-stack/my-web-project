using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace Bibliotheque.Web.Pages
{
    public class LivresImportModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public LivresImportModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        public string? Message { get; set; }
        public string? Error { get; set; }

        public IActionResult OnGet()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Livres");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var jwt = HttpContext.Session.GetString("jwt");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN")
                return RedirectToPage("/Livres");

            if (UploadFile == null || UploadFile.Length == 0)
            {
                Error = "Choisissez un fichier Excel.";
                return Page();
            }

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            using var form = new MultipartFormDataContent();
            using var stream = UploadFile.OpenReadStream();
            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(UploadFile.ContentType)
                        ? "application/octet-stream"
                        : UploadFile.ContentType
                );

            form.Add(fileContent, "file", UploadFile.FileName);

            var resp = await client.PostAsync($"{apiBase}/api/Livres/import", form);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Error = $"Erreur lors de l'import: {body}";
                return Page();
            }

            Message = "Importation réussie.";
            return RedirectToPage("/Livres");
        }
    }
}