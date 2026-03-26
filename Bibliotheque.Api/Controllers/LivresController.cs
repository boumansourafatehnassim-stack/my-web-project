using Bibliotheque.Api.Data;
using Bibliotheque.Api.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


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

        public class LivreListItemDto
        {
            public int Id { get; set; }
            public string Titre { get; set; } = string.Empty;
            public string Auteur { get; set; } = string.Empty;
            public string? AdresseBibliogr { get; set; }
            public int? AnneePublication { get; set; }
            public string? Langue { get; set; }
            public string? Theme { get; set; }
            public string? Cote { get; set; }
            public int NombreExemplaires { get; set; }
            public int NombreDisponibles { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetLivres(
     [FromQuery] string? recherche,
     [FromQuery] string? theme,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 20)
        {
            if (page < 1)
                page = 1;

            if (pageSize < 1)
                pageSize = 20;

            if (pageSize > 100)
                pageSize = 100;

            var query = _db.Livres
                .Where(l => !l.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                query = query.Where(l =>
                    l.Titre.Contains(recherche) ||
                    l.Auteur.Contains(recherche));
            }

            if (!string.IsNullOrWhiteSpace(theme))
            {
                query = query.Where(l => l.Theme != null && l.Theme.Contains(theme));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LivreListItemDto
                {
                    Id = l.Id,
                    Titre = l.Titre,
                    Auteur = l.Auteur,
                    AdresseBibliogr = l.AdresseBibliogr,
                    AnneePublication = l.AnneePublication,
                    Langue = l.Langue,
                    Theme = l.Theme,
                    NombreExemplaires = l.Exemplaires.Count,
                    NombreDisponibles = l.Exemplaires.Count(e => e.Statut == "DISPONIBLE")
                })
                .ToListAsync();

            var result = new PagedResult<LivreListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(result);
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Fichier manquant");

            int totalLivres = 0;
            int totalExemplaires = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var wb = new XLWorkbook(stream);

            foreach (var ws in wb.Worksheets)
            {
                var range = ws.RangeUsed();
                if (range == null)
                    continue;

                int lastRow = range.LastRow().RowNumber();

                for (int r = 2; r <= lastRow; r++)
                {
                    var row = ws.Row(r);

                    // B=Titre, C=Auteur, D=Adresse, E=Année, F=Langue, G=Cote, H=Observation
                    var titre = row.Cell(2).GetString().Trim();
                    var auteur = row.Cell(3).GetString().Trim();
                    var adresse = row.Cell(4).GetString().Trim();
                    var anneeRaw = row.Cell(5).GetString().Trim();
                    var langue = row.Cell(6).GetString().Trim();
                    var cote = row.Cell(7).GetString().Trim();
                    var obs = row.Cell(8).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(titre))
                        continue;

                    if (string.IsNullOrWhiteSpace(auteur))
                        auteur = "(S.A)";

                    int? annee = null;
                    var digits = new string(anneeRaw.Where(char.IsDigit).Take(4).ToArray());
                    if (int.TryParse(digits, out var y))
                        annee = y;

                    bool exists = await _db.Livres.AnyAsync(x =>
                        !x.IsDeleted &&
                        x.Titre == titre &&
                        x.Auteur == auteur);

                    if (exists)
                        continue;

                    var livre = new Livre
                    {
                        Titre = titre,
                        Auteur = auteur,
                        Theme = ws.Name, // اسم الورقة يتحط في Theme
                        AnneePublication = annee,
                        Langue = langue,
                        AdresseBibliogr = adresse,
                        IsDeleted = false
                    };

                    _db.Livres.Add(livre);
                    await _db.SaveChangesAsync();

                    totalLivres++;

                    int nbEx = 1;
                    var match = Regex.Match(obs.ToLower(), @"(\d+)");
                    if (match.Success && int.TryParse(match.Value, out var n))
                        nbEx = Math.Min(n, 20);

                    for (int i = 1; i <= nbEx; i++)
                    {
                        _db.Exemplaires.Add(new Exemplaire
                        {
                            LivreId = livre.Id,
                            CodeBarres = $"BC-{livre.Id}-{i:D3}",
                            Statut = "DISPONIBLE",
                            Emplacement = cote
                        });

                        totalExemplaires++;
                    }

                    await _db.SaveChangesAsync();
                }
            }

            return Ok(new
            {
                totalLivres,
                totalExemplaires
            });
        }
    }
}