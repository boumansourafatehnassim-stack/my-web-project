using Bibliotheque.Api.Data;
using Bibliotheque.Api.Dtos;
using Bibliotheque.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DemandesController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public DemandesController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // ✅ 1. للمكتبي: جلب كل الطلبات المعلقة (EN_ATTENTE)
        [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetEnAttente()
        {
            var demandes = await _db.DemandesEmprunt
                .Where(d => (d.Statut ?? "").Trim().ToUpper() == "EN_ATTENTE")
                .Join(_db.Users, d => d.UserId, u => u.Id, (d, u) => new { d, u })
                .Join(_db.Exemplaires, du => du.d.ExemplaireId, e => e.Id, (du, e) => new { du.d, du.u, e })
                .Join(_db.Livres, due => due.e.LivreId, l => l.Id, (due, l) => new
                {
                    due.d.Id,
                    due.d.UserId,
                    due.d.ExemplaireId,
                    due.d.DateDemande,
                    due.d.Statut,
                    due.d.Commentaire,
                    due.u.Nom,
                    due.u.Prenom,
                    due.u.Email,
                    due.u.Matricule,
                    due.u.PhotoPath,
                    LivreTitre = l.Titre
                })
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(demandes);
        }

        // ✅ 2. للطالب: جلب طلباته الشخصية فقط
        // GET /api/Demandes/user/34
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetUserDemandes(int userId)
        {
            // تأكد أن الطالب يطلب طلباته هو فقط (لحماية الخصوصية)
            var currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (role != "BIBLIOTHECAIRE" && role != "ADMIN" && currentUserId != userId)
            {
                return Forbid();
            }

            var demandes = await _db.DemandesEmprunt
                .Where(d => d.UserId == userId)
                .Join(_db.Exemplaires, d => d.ExemplaireId, e => e.Id, (d, e) => new { d, e })
                .Join(_db.Livres, de => de.e.LivreId, l => l.Id, (de, l) => new
                {
                    de.d.Id,
                    de.d.UserId,
                    de.d.ExemplaireId,
                    de.d.DateDemande,
                    de.d.Statut,
                    de.d.Commentaire,
                    LivreTitre = l.Titre
                })
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(demandes);
        }

        // ✅ بقية الدوال (Demander, Traiter) تبقى كما هي...
        [HttpPost]
        public async Task<IActionResult> Demander([FromBody] DemandeEmpruntRequest req)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            int userId = int.Parse(userIdClaim);
            try
            {
                var result = await _db.DemanderEmpruntAsync(userId, req.LivreId, req.Commentaire);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
        [HttpPost("traiter")]
        public async Task<IActionResult> Traiter([FromBody] TraiterDemandeRequest req)
        {
            var demande = await _db.DemandesEmprunt.AsNoTracking().FirstOrDefaultAsync(d => d.Id == req.DemandeId);
            if (demande == null) return NotFound(new { error = "Demande غير موجودة." });
            try
            {
                var action = (req.Action ?? "").Trim().ToUpper();
                var result = await _db.TraiterDemandeAsync(req.DemandeId, action);
                // كود الـ Notifications كما هو عندك...
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}