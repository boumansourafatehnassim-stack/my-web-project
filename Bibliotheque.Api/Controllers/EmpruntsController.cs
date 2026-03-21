using Bibliotheque.Api.Data;
using Bibliotheque.Api.Dtos;
using Bibliotheque.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Bibliotheque.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmpruntsController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public EmpruntsController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // ✅ POST /api/Emprunts/retour
        // Body: { empruntId: 123 }
        [Authorize(Roles = "BIBLIOTHECAIRE")]
        [HttpPost("retour")]
        public async Task<IActionResult> Retour([FromBody] RetourRequest req)
        {
            if (req == null || req.EmpruntId <= 0)
                return BadRequest(new { error = "EmpruntId غير صحيح." });

            // ✅ نجيب emprunt قبل SP باش نعرف UserId
            var emprunt = await _db.Emprunts
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == req.EmpruntId);

            if (emprunt == null)
                return NotFound(new { error = "Emprunt غير موجود." });

            try
            {
                // ✅ تنفذ SP
                var result = await _db.EnregistrerRetourAsync(req.EmpruntId);

                // ✅ Notification للطالب (حقول أساسية فقط باش ما نطيحوش في مشاكل DB)
                _db.Notifications.Add(new Notification
                {
                    UserId = emprunt.UserId,
                    Message = "Votre retour a été enregistré avec succès. Merci d'avoir rendu le livre.",
                    Type = "RETOUR",
                    DateCreation = DateTime.UtcNow,
                    Lu = false
                });

                await _db.SaveChangesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        // GET: /api/emprunts/user/1/en-cours
        [HttpGet("user/{userId:int}/en-cours")]
        public async Task<IActionResult> GetEnCours(int userId)
        {
            var list = await _db.Emprunts
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.DateRetourReelle == null)
                .OrderByDescending(e => e.DateEmprunt)
                .ToListAsync();

            return Ok(list);
        }

        // GET: /api/emprunts/user/1/historique
        [HttpGet("user/{userId:int}/historique")]
        public async Task<IActionResult> GetHistorique(int userId)
        {
            var list = await _db.Emprunts
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.DateRetourReelle != null)
                .OrderByDescending(e => e.DateRetourReelle)
                .ToListAsync();

            return Ok(list);
        }
    }
}