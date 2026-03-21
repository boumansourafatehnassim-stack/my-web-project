using Bibliotheque.Api.Data;
using Bibliotheque.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
    public class InscriptionDemandesController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public InscriptionDemandesController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // GET: api/InscriptionDemandes?statut=EN_ATTENTE
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string statut = "EN_ATTENTE")
        {
            var demandes = await _db.DemandesInscription
                .Where(d => d.Statut == statut)
                .Join(_db.Users,
                    d => d.UserId,
                    u => u.Id,
                    (d, u) => new
                    {
                        d.Id,
                        d.UserId,
                        u.Nom,
                        u.Prenom,
                        u.Email,
                        u.Matricule,
                        u.PhotoPath,
                        d.DateDemande,
                        d.Statut
                    })
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(demandes);

        }
        [HttpPost("traiter")]
        public async Task<IActionResult> Traiter([FromBody] TraiterDemandeRequest req)
        {
            var demande = await _db.DemandesInscription
                .FirstOrDefaultAsync(d => d.Id == req.DemandeId);

            if (demande == null)
                return NotFound("Demande introuvable");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == demande.UserId);
            if (user == null)
                return NotFound("Utilisateur introuvable");

            if (req.Action == "VALIDER")
            {
                demande.Statut = "VALIDEE";
                user.IsActive = true;

                if (req.CreerCarte && user.DateCreationCarte == null)
                {
                    user.DateCreationCarte = DateTime.UtcNow;
                    user.DateExpirationCarte = user.DateCreationCarte.Value.AddYears(5);
                }
            }
            else if (req.Action == "REFUSER")
            {
                demande.Statut = "REFUSEE";
                user.IsActive = false;
            }
            else
            {
                return BadRequest("Action invalide");
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Demande traitée avec succès" });
        }
    }
}
