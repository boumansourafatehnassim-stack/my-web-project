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
    [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
    public class RetoursController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public RetoursController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // GET: api/Retours/en-cours/7
        [HttpGet("en-cours/{userId:int}")]
        public async Task<IActionResult> GetEmpruntsEnCours(int userId)
        {
            var emprunts = await _db.Emprunts
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.Statut == "EN_COURS")
                .OrderByDescending(e => e.Id)
                .Select(e => new
                {
                    e.Id,
                    e.ExemplaireId,
                    e.DateEmprunt,
                    e.DateRetourPrevue,
                    e.Statut
                })
                .ToListAsync();

            return Ok(emprunts);
        }

        // POST: api/Retours
        [HttpPost]
        public async Task<IActionResult> EnregistrerRetour([FromBody] RetourRequest req)
        {
            var emprunt = await _db.Emprunts
                .FirstOrDefaultAsync(e => e.Id == req.EmpruntId);

            if (emprunt == null)
                return NotFound(new { error = "Emprunt introuvable." });

            if (emprunt.Statut != "EN_COURS")
                return BadRequest(new { error = "Cet emprunt n'est pas en cours." });

            var exemplaire = await _db.Exemplaires
                .FirstOrDefaultAsync(x => x.Id == emprunt.ExemplaireId);

            if (exemplaire == null)
                return NotFound(new { error = "Exemplaire introuvable." });

            emprunt.Statut = "RETOURNE";
            emprunt.DateRetourReelle = DateTime.UtcNow;

            exemplaire.Statut = "DISPONIBLE";

            _db.Notifications.Add(new Notification
            {
                UserId = emprunt.UserId,
                Message = "Votre retour a été enregistré avec succès.",
                DateCreation = DateTime.UtcNow,
                
            });

            await _db.SaveChangesAsync();

            return Ok(new { message = "Retour enregistré avec succès." });
        }
    }
}