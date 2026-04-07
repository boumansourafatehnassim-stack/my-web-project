using Bibliotheque.Api.Data;
using Bibliotheque.Api.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

        public class AjouterLivreDto
        {
            public string Titre { get; set; } = "";
            public string Auteur { get; set; } = "";
            public string? Theme { get; set; }
            public int? AnneePublication { get; set; }
            public int NombreExemplaires { get; set; }
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
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AjouterLivre([FromBody] AjouterLivreDto dto)
        {
            if (dto == null)
                return BadRequest("Données invalides");

            if (string.IsNullOrWhiteSpace(dto.Titre) || string.IsNullOrWhiteSpace(dto.Auteur))
                return BadRequest("Titre et Auteur obligatoires");

            if (dto.NombreExemplaires < 1)
                return BadRequest("Nombre d'exemplaires invalide");

            var livreExistant = await _db.Livres.AnyAsync(l =>
                !l.IsDeleted &&
                l.Titre == dto.Titre &&
                l.Auteur == dto.Auteur);

            if (livreExistant)
                return BadRequest("Ce livre existe déjà");

            var livre = new Livre
            {
                Titre = dto.Titre.Trim(),
                Auteur = dto.Auteur.Trim(),
                Theme = string.IsNullOrWhiteSpace(dto.Theme) ? null : dto.Theme.Trim(),
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
                    Emplacement = dto.Theme ?? "Aucun"
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Livre ajouté avec succès",
                livreId = livre.Id
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteLivre(int id)
        {
            var livre = await _db.Livres
                .Include(l => l.Exemplaires)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (livre == null)
                return NotFound("Livre introuvable");

            var hasActiveLoans = await _db.Emprunts
                .AnyAsync(e => livre.Exemplaires.Select(ex => ex.Id).Contains(e.ExemplaireId) && e.DateRetourReelle == null);

            if (hasActiveLoans)
                return BadRequest("Impossible de supprimer ce livre car il a des emprunts en cours");

            _db.Exemplaires.RemoveRange(livre.Exemplaires);
            _db.Livres.Remove(livre);

            await _db.SaveChangesAsync();

            return Ok(new { message = "Livre supprimé avec succès" });
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLivreById(int id)
        {
            var livre = await _db.Livres
                .Where(l => !l.IsDeleted && l.Id == id)
                .Select(l => new AjouterLivreDto
                {
                    Titre = l.Titre,
                    Auteur = l.Auteur,
                    Theme = l.Theme,
                    AnneePublication = l.AnneePublication,
                    NombreExemplaires = l.Exemplaires.Count
                })
                .FirstOrDefaultAsync();

            if (livre == null)
                return NotFound("Livre introuvable");

            return Ok(livre);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ModifierLivre(int id, [FromBody] AjouterLivreDto dto)
        {
            var livre = await _db.Livres
                .Include(l => l.Exemplaires)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (livre == null)
                return NotFound("Livre introuvable");

            if (string.IsNullOrWhiteSpace(dto.Titre) || string.IsNullOrWhiteSpace(dto.Auteur))
                return BadRequest("Titre et Auteur obligatoires");

            if (dto.NombreExemplaires < 1)
                return BadRequest("Nombre d'exemplaires invalide");

            livre.Titre = dto.Titre.Trim();
            livre.Auteur = dto.Auteur.Trim();
            livre.Theme = string.IsNullOrWhiteSpace(dto.Theme) ? null : dto.Theme.Trim();
            livre.AnneePublication = dto.AnneePublication;

            var currentCount = livre.Exemplaires.Count;
            var wantedCount = dto.NombreExemplaires;

            if (wantedCount > currentCount)
            {
                for (int i = currentCount + 1; i <= wantedCount; i++)
                {
                    _db.Exemplaires.Add(new Exemplaire
                    {
                        LivreId = livre.Id,
                        CodeBarres = $"BC-{livre.Id}-{i:D3}",
                        Statut = "DISPONIBLE",
                        Emplacement = dto.Theme ?? "Aucun"
                    });
                }
            }
            else if (wantedCount < currentCount)
            {
                var exemplairesASupprimer = livre.Exemplaires
                    .Where(e => e.Statut == "DISPONIBLE")
                    .OrderByDescending(e => e.Id)
                    .Take(currentCount - wantedCount)
                    .ToList();

                if (exemplairesASupprimer.Count < (currentCount - wantedCount))
                    return BadRequest("Impossible de réduire le nombre d'exemplaires car certains sont empruntés");

                _db.Exemplaires.RemoveRange(exemplairesASupprimer);
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Livre modifié avec succès" });
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
                        Theme = ws.Name,
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
                            Emplacement = string.IsNullOrWhiteSpace(cote) ? "Aucun" : cote
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