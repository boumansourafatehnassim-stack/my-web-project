using Bibliotheque.Api.Data;
using Bibliotheque.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;

        public NotificationsController(BibliothequeDbContext db)
        {
            _db = db;
        }

        // GET: /api/notifications/user/1
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var list = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();

            return Ok(list);
        }

        // POST: /api/notifications/mark-read
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest req)
        {
            var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == req.Id);
            if (notif == null)
                return NotFound(new { error = "Notification introuvable" });

         
            await _db.SaveChangesAsync();

            return Ok(new { message = "Notification marquée comme lue" });
        }

        public class MarkReadRequest
        {
            public int Id { get; set; }
        }
        // GET: api/notifications/count/1
        [HttpGet("count/{userId:int}")]
        public async Task<IActionResult> GetCount(int userId)
        {
            var count = await _db.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId);

            return Ok(new { count });
        }
    }
}