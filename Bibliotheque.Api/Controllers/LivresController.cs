using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Bibliotheque.Api.Data;
using Bibliotheque.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LivresController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public LivresController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // ✅ Model خاص بالـUpload
        public class ImportLivresRequest
        {
            public IFormFile File { get; set; } = default!;
        }

        // ✅ Model خاص بالإضافة اليدوية
        public class CreateLivreRequest
        {
            public string Titre { get; set; } = "";
            public string Auteur { get; set; } = "";
            public string? Theme { get; set; }
            public int? AnneePublication { get; set; }
            public int NombreExemplaires { get; set; }
        }

        // ✅ عرض كل الكتب
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? theme,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 1000)
        {
            if (page < 1) page = 1;
            if (pageSize > 1000) pageSize = 1000;

            var q = _db.Livres.AsNoTracking().AsQueryable();

            // ✅ Soft Delete
            q = q.Where(x => x.IsDeleted == false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                q = q.Where(x => x.Titre.Contains(search) || x.Auteur.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(theme))
            {
                theme = theme.Trim();
                q = q.Where(x => x.Theme != null && x.Theme == theme);
            }

            var total = await q.CountAsync();

            var items = await q
    .OrderBy(x => x.Id)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(x => new
    {
        x.Id,
        x.Titre,
        x.Auteur,
        x.Theme,
        x.AnneePublication,
        x.IsDeleted,
        NombreExemplaires = _db.Exemplaires.Count(e => e.LivreId == x.Id),
        NombreDisponibles = _db.Exemplaires.Count(e => e.LivreId == x.Id && e.Statut == "DISPONIBLE")
    })
    .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // ✅ استيراد كتب من Excel + توليد Exemplaires حسب OBSERVATION
        [Authorize(Roles = "BIBLIOTHECAIRE")]
        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportExcel([FromForm] ImportLivresRequest request)
        {
            var file = request.File;

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Fichier Excel manquant." });

            var addedLivres = 0;
            var addedExemplaires = 0;
            var errors = new List<object>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            {
                // ✅ مطابقة أعمدة Excel مع قاعدة البيانات:
                // B = TITRE / TITRE DE PARTIE
                // C = AUTEUR / COLLECTIVITE
                // D = ADRESSE BIBLIOGR.
                // E = ANNEE D'EDITION
                // F = LANGUE
                // G = COTE
                // H = OBSERVATION (عدد النسخ)

                var titre = ws.Cell(r, 2).GetString().Trim();   // B
                var auteur = ws.Cell(r, 3).GetString().Trim();  // C
                var adresseBibliogr = ws.Cell(r, 4).GetString().Trim(); // D
                var langue = ws.Cell(r, 6).GetString().Trim();  // F

                // Theme غير موجود في ملفك
                string? theme = null;

                int? annee = null;
                var anneeCell = ws.Cell(r, 5); // E
                if (!anneeCell.IsEmpty())
                {
                    if (anneeCell.DataType == XLDataType.Number)
                        annee = (int)anneeCell.GetDouble();
                    else if (int.TryParse(anneeCell.GetString().Trim(), out var y))
                        annee = y;
                }

                var cote = ws.Cell(r, 7).GetString().Trim();        // G
                var observation = ws.Cell(r, 8).GetString().Trim(); // H

                if (string.IsNullOrWhiteSpace(titre) || string.IsNullOrWhiteSpace(auteur))
                {
                    errors.Add(new { row = r, error = "Titre/Auteur obligatoire." });
                    continue;
                }

                int nbEx = 1;
                if (!string.IsNullOrWhiteSpace(observation))
                {
                    var digits = new string(observation.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out var n) && n > 0)
                        nbEx = n;
                }

                var exists = await _db.Livres.AnyAsync(x =>
                    x.Titre == titre &&
                    x.Auteur == auteur &&
                    x.IsDeleted == false);

                if (exists)
                {
                    errors.Add(new { row = r, error = "Livre déjà موجود (même titre + auteur)." });
                    continue;
                }

                var livre = new Livre
                {
                    Titre = titre,
                    Auteur = auteur,
                    Theme = theme,
                    AnneePublication = annee,
                    Langue = string.IsNullOrWhiteSpace(langue) ? null : langue,
                    AdresseBibliogr = string.IsNullOrWhiteSpace(adresseBibliogr) ? null : adresseBibliogr,
                    IsDeleted = false
                };

                _db.Livres.Add(livre);
                await _db.SaveChangesAsync();

                addedLivres++;

                for (int i = 1; i <= nbEx; i++)
                {
                    _db.Exemplaires.Add(new Exemplaire
                    {
                        LivreId = livre.Id,
                        CodeBarres = $"BC-{livre.Id}-{i:D3}",
                        Statut = "DISPONIBLE",
                        Emplacement = string.IsNullOrWhiteSpace(cote) ? null : cote
                    });

                    addedExemplaires++;
                }

                await _db.SaveChangesAsync();
            }

            return Ok(new
            {
                addedLivres,
                addedExemplaires,
                errorsCount = errors.Count,
                errors
            });
        }

        // ✅ إضافة كتاب يدويًا + إنشاء Exemplaires
        [Authorize(Roles = "BIBLIOTHECAIRE")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLivreRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Titre) || string.IsNullOrWhiteSpace(dto.Auteur))
                return BadRequest(new { error = "Titre و Auteur لازم." });

            if (dto.NombreExemplaires < 1)
                return BadRequest(new { error = "Nombre d'exemplaires لازم يكون 1 أو أكثر." });

            var titre = dto.Titre.Trim();
            var auteur = dto.Auteur.Trim();
            var theme = string.IsNullOrWhiteSpace(dto.Theme) ? null : dto.Theme.Trim();

            var exists = await _db.Livres.AnyAsync(x =>
                x.Titre == titre &&
                x.Auteur == auteur &&
                x.IsDeleted == false);

            if (exists)
                return BadRequest(new { error = "Livre déjà موجود (même titre + auteur)." });

            var livre = new Livre
            {
                Titre = titre,
                Auteur = auteur,
                Theme = theme,
                AnneePublication = dto.AnneePublication,
                IsDeleted = false
            };

            _db.Livres.Add(livre);
            await _db.SaveChangesAsync();

            for (int i = 1; i <= dto.NombreExemplaires; i++)
            {
                _db.Exemplaires.Add(new Exemplaire
                {
                    LivreId = livre.Id,
                    CodeBarres = $"BC-{livre.Id}-{i:D3}",
                    Statut = "DISPONIBLE",
                    Emplacement = null
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                livre,
                nombreExemplaires = dto.NombreExemplaires
            });
        }

        // ✅ تعديل كتاب
        [Authorize(Roles = "BIBLIOTHECAIRE")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Livre dto)
        {
            var livre = await _db.Livres.FindAsync(id);
            if (livre == null)
                return NotFound(new { error = "Livre غير موجود." });

            if (string.IsNullOrWhiteSpace(dto.Titre) || string.IsNullOrWhiteSpace(dto.Auteur))
                return BadRequest(new { error = "Titre و Auteur لازم." });

            livre.Titre = dto.Titre.Trim();
            livre.Auteur = dto.Auteur.Trim();
            livre.Theme = dto.Theme;
            livre.AnneePublication = dto.AnneePublication;

            await _db.SaveChangesAsync();
            return Ok(livre);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var livre = await _db.Livres.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (livre == null)
                return NotFound(new { error = "Livre غير موجود." });

            return Ok(livre);
        }

        // ✅ حذف كتاب (Soft Delete)
        [Authorize(Roles = "BIBLIOTHECAIRE")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var livre = await _db.Livres.FindAsync(id);
            if (livre == null)
                return NotFound(new { error = "Livre غير موجود." });

            livre.IsDeleted = true;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Livre supprimé." });
        }
    }
}