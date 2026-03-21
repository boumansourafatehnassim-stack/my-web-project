using Bibliotheque.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public DashboardController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _db.Users.CountAsync();
            var activeUsers = await _db.Users.CountAsync(u => u.IsActive);
            var totalLivres = await _db.Livres.CountAsync();
            var totalDemandes = await _db.DemandesEmprunt.CountAsync();
            var demandesEnAttente = await _db.DemandesEmprunt.CountAsync(d => d.Statut == "EN_ATTENTE");
            var totalEmprunts = await _db.Emprunts.CountAsync();
            var empruntsEnCours = await _db.Emprunts.CountAsync(e => e.Statut == "EN_COURS");
            var totalNotifications = await _db.Notifications.CountAsync();

            return Ok(new
            {
                totalUsers,
                activeUsers,
                totalLivres,
                totalDemandes,
                demandesEnAttente,
                totalEmprunts,
                empruntsEnCours,
                totalNotifications
            });
        }
    }
}