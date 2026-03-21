using Bibliotheque.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
    public class AttestationsController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public AttestationsController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // GET: api/attestations/quitus?search=12345
        [HttpGet("quitus")]
        public async Task<IActionResult> GetQuitus([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest(new { error = "Le matricule ou l'email est obligatoire." });

            search = search.Trim().ToLower();

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Matricule.ToLower() == search ||
                    u.Email.ToLower() == search);

            if (user == null)
                return NotFound(new { error = "Étudiant introuvable." });

            var empruntsEnCours = await (
                from e in _db.Emprunts.AsNoTracking()
                join ex in _db.Exemplaires.AsNoTracking() on e.ExemplaireId equals ex.Id
                join l in _db.Livres.AsNoTracking() on ex.LivreId equals l.Id
                where e.UserId == user.Id && e.Statut == "EN_COURS"
                select new
                {
                    e.Id,
                    e.DateEmprunt,
                    e.DateRetourPrevue,
                    LivreTitre = l.Titre,
                    ex.CodeBarres
                }
            ).ToListAsync();

            var canGenerate = empruntsEnCours.Count == 0;
            var documentText =
            $@"République Algérienne Démocratique et Populaire
Ministère de l’Enseignement Supérieur et de la Recherche Scientifique
École Nationale Supérieure des Technologies de l’Information et de la Communication - ENSTICP

ATTESTATION DE QUITUS DE BIBLIOTHÈQUE

Je soussigné(e), bibliothécaire de l’ENSTICP, atteste que :

Nom : {user.Nom}
Prénom : {user.Prenom}
Email : {user.Email}
Matricule : {user.Matricule}

est en situation régulière vis-à-vis de la bibliothèque et ne détient actuellement aucun ouvrage non restitué.

La présente attestation est délivrée à l’intéressé(e) pour servir et valoir ce que de droit.

Fait à l’ENSTICP, le {DateTime.Now:dd/MM/yyyy}.

Signature du bibliothécaire
Cachet de la bibliothèque";
            return Ok(new
            {
                user.Id,
                user.Nom,
                user.Prenom,
                user.Email,
                user.Matricule,
                user.Role,
                canGenerate,
                empruntsEnCoursCount = empruntsEnCours.Count,
                empruntsEnCours,
                documentText = canGenerate ? documentText : null
            });
        }
    }
}