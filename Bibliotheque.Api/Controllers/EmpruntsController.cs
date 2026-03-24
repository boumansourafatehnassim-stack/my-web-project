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

        // ✅ الدالة الجديدة: البحث عن إعارات طالب بواسطة Matricule
        // GET: /api/emprunts/matricule/ADM-2026/en-cours
        [Authorize(Roles = "BIBLIOTHECAIRE")]
        [HttpGet("matricule/{matricule}/en-cours")]
        public async Task<IActionResult> GetEnCoursByMatricule(string matricule)
        {
            // 1. البحث عن المستخدم بواسطة رقم التسجيل
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Matricule == matricule);

            if (user == null)
                return NotFound(new { error = "Aucun étudiant trouvé avec هذا الرقم." });

            // 2. جلب الإعارات التي لم تُرجع بعد لهذا المستخدم
            var list = await _db.Emprunts
                .AsNoTracking()
                .Where(e => e.UserId == user.Id && e.DateRetourReelle == null)
                .OrderByDescending(e => e.DateEmprunt)
                .ToListAsync();

            return Ok(list);
        }

        // ✅ POST /api/Emprunts/retour
        [Authorize(Roles = "BIBLIOTHECAIRE")]
        [HttpPost("retour")]
        public async Task<IActionResult> Retour([FromBody] RetourRequest req)
        {
            if (req == null || req.EmpruntId <= 0)
                return BadRequest(new { error = "EmpruntId غير صحيح." });

            var emprunt = await _db.Emprunts
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == req.EmpruntId);

            if (emprunt == null)
                return NotFound(new { error = "Emprunt غير موجود." });

            try
            {
                var result = await _db.EnregistrerRetourAsync(req.EmpruntId);

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
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: /api/emprunts/user/{userId}/en-cours
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

        // البقية كما هي...
    }
}