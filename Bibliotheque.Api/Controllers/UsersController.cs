using Bibliotheque.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public UsersController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users
                .AsNoTracking()
                .OrderByDescending(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Nom,
                    u.Prenom,
                    u.Email,
                    u.Matricule,
                    u.Role,
                    u.IsActive,
                    u.DateCreation,
                    u.PhotoPath
                })
                .ToListAsync();

            return Ok(users);
        }

        // PUT: api/Users/{id}/toggle-active
        [HttpPut("{id:int}/toggle-active")]
        [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { error = "Utilisateur introuvable" });

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = user.IsActive ? "Compte activé avec succès" : "Compte désactivé avec succès",
                user.Id,
                user.IsActive
            });
        }

        // PUT: api/Users/{id}/reset-password
        [HttpPut("{id:int}/reset-password")]
        [Authorize(Roles = "BIBLIOTHECAIRE,ADMIN")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { error = "Le nouveau mot de passe est obligatoire" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound(new { error = "Utilisateur introuvable" });

            user.MotDePasseHash = req.NewPassword;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Mot de passe modifié avec succès" });
        }

        public class ResetPasswordRequest
        {
            public string NewPassword { get; set; } = "";
        }

        // GET: api/Users/{id}/carte
        [HttpGet("{id:int}/carte")]
        public async Task<IActionResult> GetCarte(int id)
        {
            var currentUserIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var currentRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(currentUserIdClaim))
                return Unauthorized(new { error = "Utilisateur non authentifié" });

            var currentUserId = int.Parse(currentUserIdClaim);

            // الطالب يشوف غير بطاقتو، والمكتبي/الأدمن يشوفو الجميع
            if (currentRole != "BIBLIOTHECAIRE" && currentRole != "ADMIN" && currentUserId != id)
                return Forbid();

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { error = "Utilisateur introuvable" });

            if (user.DateCreationCarte == null)
                return BadRequest(new { error = "Aucune carte n'a été créée pour cet utilisateur." });

            return Ok(new
            {
                user.Id,
                user.Nom,
                user.Prenom,
                user.Email,
                user.Matricule,
                user.PhotoPath,
                user.DateNaissance,
                user.DateCreationCarte,
                user.DateExpirationCarte,
                user.CarteImprimee,
                user.DateImpressionCarte,
                user.IsActive,
                user.Role
            });
        }

        // PUT: api/Users/{id}/marquer-impression-carte
        [HttpPut("{id:int}/marquer-impression-carte")]
        public async Task<IActionResult> MarquerImpressionCarte(int id)
        {
            var currentUserIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var currentRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(currentUserIdClaim))
                return Unauthorized(new { error = "Utilisateur non authentifié" });

            var currentUserId = int.Parse(currentUserIdClaim);

            // الطالب يطبع غير بطاقتو، والمكتبي/الأدمن يقدرو يطبعو أي بطاقة
            if (currentRole != "BIBLIOTHECAIRE" && currentRole != "ADMIN" && currentUserId != id)
                return Forbid();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { error = "Utilisateur introuvable" });

            if (user.DateCreationCarte == null)
                return BadRequest(new { error = "Aucune carte n'a été créée pour cet utilisateur." });

            if (user.CarteImprimee)
                return BadRequest(new { error = "La carte a déjà été imprimée." });

            user.CarteImprimee = true;
            user.DateImpressionCarte = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Première impression enregistrée avec succès." });
        }
    }
}