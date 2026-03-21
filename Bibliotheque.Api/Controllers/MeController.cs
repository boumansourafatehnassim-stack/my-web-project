using Bibliotheque.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public MeController(BibliothequeDbContext db)
        {
            _db = db;
        }

        private int GetUserId()
        {
            var v = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(v))
                throw new Exception("Token invalide: userId manquant");

            return int.Parse(v);
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> Notifications()
        {
            int userId = GetUserId();

            var list = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("emprunts/en-cours")]
        public async Task<IActionResult> EmpruntsEnCours()
        {
            int userId = GetUserId();

            var list = await _db.Emprunts
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.DateRetourReelle == null)
                .OrderByDescending(e => e.DateEmprunt)
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("emprunts/historique")]
        public async Task<IActionResult> EmpruntsHistorique()
        {
            int userId = GetUserId();

            var list = await _db.Emprunts
                .AsNoTracking()
                .Where(e => e.UserId == userId && e.DateRetourReelle != null)
                .OrderByDescending(e => e.DateRetourReelle)
                .ToListAsync();

            return Ok(list);
        }
    }
}
