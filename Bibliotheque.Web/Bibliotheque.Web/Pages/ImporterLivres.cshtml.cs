using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bibliotheque.Web.Pages
{
    public class ImporterLivresModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ImporterLivresModel(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public string? Message { get; set; }
        public string? Error { get; set; }

        public List<ImportError> Errors { get; set; } = new();

        public class ImportError
        {
            public int Row { get; set; }
            public string Error { get; set; } = "";
        }

        public IActionResult OnGet()
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE")
                return RedirectToPage("/Livres");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(IFormFile ExcelFile)
        {
            var role = HttpContext.Session.GetString("role");
            if (role != "BIBLIOTHECAIRE")
                return RedirectToPage("/Livres");

            var jwt = HttpContext.Session.GetString("jwt");
            if (string.IsNullOrEmpty(jwt))
                return RedirectToPage("/Login");

            if (ExcelFile == null || ExcelFile.Length == 0)
            {
                Error = "اختار ملف Excel (.xlsx).";
                return Page();
            }

            var apiBase = _config["Api:BaseUrl"]!.TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            using var form = new MultipartFormDataContent();
            using var fs = ExcelFile.OpenReadStream();
            var fileContent = new StreamContent(fs);

            // إذا ContentType فاضي نخليها الافتراضي
            var contentType = string.IsNullOrWhiteSpace(ExcelFile.ContentType)
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : ExcelFile.ContentType;

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            // ✅ مهم: اسم المفتاح لازم يطابق ImportLivresRequest.File => "File"
            form.Add(fileContent, "File", ExcelFile.FileName);

            var resp = await client.PostAsync($"{apiBase}/api/Livres/import", form);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Error = $"فشل الاستيراد: {body}";
                return Page();
            }

            // ✅ قراءة النتيجة بأمان
            using var doc = JsonDocument.Parse(body);

            int addedLivres = 0;
            int addedExemplaires = 0;
            int errorsCount = 0;

            if (doc.RootElement.TryGetProperty("addedLivres", out var pLivres) && pLivres.ValueKind == JsonValueKind.Number)
                addedLivres = pLivres.GetInt32();

            if (doc.RootElement.TryGetProperty("addedExemplaires", out var pEx) && pEx.ValueKind == JsonValueKind.Number)
                addedExemplaires = pEx.GetInt32();

            if (doc.RootElement.TryGetProperty("errorsCount", out var pErr) && pErr.ValueKind == JsonValueKind.Number)
                errorsCount = pErr.GetInt32();

            Errors = new List<ImportError>();

            if (doc.RootElement.TryGetProperty("errors", out var pErrors) && pErrors.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in pErrors.EnumerateArray())
                {
                    int row = 0;
                    string errMsg = "";

                    if (e.TryGetProperty("row", out var pr) && pr.ValueKind == JsonValueKind.Number)
                        row = pr.GetInt32();

                    if (e.TryGetProperty("error", out var pe) && pe.ValueKind == JsonValueKind.String)
                        errMsg = pe.GetString() ?? "";

                    Errors.Add(new ImportError { Row = row, Error = errMsg });
                }
            }

            Message = $"✅ تمت إضافة {addedLivres} كتاب و {addedExemplaires} نسخة (Exemplaires). أخطاء: {errorsCount}.";
            return Page();
        }
    }
}