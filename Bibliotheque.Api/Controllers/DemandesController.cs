using Bibliotheque.Api.Data;
using Bibliotheque.Api.Dtos;
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

        // ✅ GET /api/Demandes (المكتبي يشوف EN_ATTENTE)
        [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetEnAttente()
        {
            var demandes = await _db.DemandesEmprunt
                .Where(d => (d.Statut ?? "").Trim().ToUpper() == "EN_ATTENTE")
                .Join(_db.Users,
                    d => d.UserId,
                    u => u.Id,
                    (d, u) => new { d, u })
                .Join(_db.Exemplaires,
                    du => du.d.ExemplaireId,
                    e => e.Id,
                    (du, e) => new { du.d, du.u, e })
                .Join(_db.Livres,
                    due => due.e.LivreId,
                    l => l.Id,
                    (due, l) => new
                    {
                        due.d.Id,
                        due.d.UserId,
                        due.d.ExemplaireId,
                        due.d.DateDemande,
                        due.d.Statut,
                        due.d.Commentaire,

                        // معلومات المستخدم
                        due.u.Nom,
                        due.u.Prenom,
                        due.u.Email,
                        due.u.Matricule,
                        due.u.PhotoPath,

                        // معلومات الكتاب
                        LivreTitre = l.Titre
                    })
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return Ok(demandes);
        }

        // ✅ POST /api/Demandes (Demander) -> يستعمل SP
        [HttpPost]
        public async Task<IActionResult> Demander([FromBody] DemandeEmpruntRequest req)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { error = "Token invalide" });

            int userId = int.Parse(userIdClaim);

            try
            {
                var result = await _db.DemanderEmpruntAsync(userId, req.LivreId, req.Commentaire);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST /api/Demandes/traiter
        [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
        [HttpPost("traiter")]
        public async Task<IActionResult> Traiter([FromBody] TraiterDemandeRequest req)
        {
            // ✅ نجيب الطلب قبل المعالجة باش نعرف UserId
            var demande = await _db.DemandesEmprunt
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == req.DemandeId);

            if (demande == null)
                return NotFound(new { error = "Demande غير موجودة." });

            try
            {
                var action = (req.Action ?? "").Trim().ToUpper();

                // ✅ المعالجة عبر SP
                var result = await _db.TraiterDemandeAsync(req.DemandeId, action);

                // ✅ بعد نجاح المعالجة: نضيف Notification للطالب
                // نعتمد على action أو على result.Statut
                string messageFr = "";
                string type = "";

                if (action == "VALIDER")
                {
                    messageFr = "Votre demande d'emprunt a été acceptée. Veuillez vous rendre à la bibliothèque pour récupérer votre livre.";
                    type = "DEMANDE_VALIDEE";
                }
                else if (action == "REFUSER")
                {
                    messageFr = "Votre demande d'emprunt a été refusée.";
                    type = "DEMANDE_REFUSEE";
                }
                if (!string.IsNullOrEmpty(messageFr))
                {
                    try
                    {
                        _db.Notifications.Add(new Bibliotheque.Api.Models.Notification
                        {
                            UserId = demande.UserId,
                            Message = messageFr,
                            DateCreation = DateTime.UtcNow,
                            Lu = false,
                            Type = type,
                            SujetDemandeId = req.DemandeId
                        });

                        await _db.SaveChangesAsync();
                    }
                    catch
                    {
                        // ما نخلوش فشل notification يكسر قبول/رفض الطلب
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }
    }
}