using Bibliotheque.Api.Data;
using Bibliotheque.Api.Dtos;
using Bibliotheque.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bibliotheque.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly BibliothequeDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(BibliothequeDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null)
                return Unauthorized(new { error = "Email أو كلمة السر خاطئة" });

            if (user.MotDePasseHash != req.MotDePasse)
                return Unauthorized(new { error = "Email أو كلمة السر خاطئة" });

            if (!user.IsActive)
                return Unauthorized(new { error = "Compte en attente de validation" });

            var token = GenerateJwt(user.Id, user.Email, user.Role);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Nom,
                    user.Prenom,
                    user.Email,
                    user.Role,
                    user.IsActive
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDto dto)
        {
            var exists = await _db.Users.AnyAsync(u =>
    u.Email.ToLower().Trim() == dto.Email.ToLower().Trim()
);
            if (exists)
                return BadRequest(new { error = "Email déjà utilisé" });

            var role = string.IsNullOrWhiteSpace(dto.Role)
                ? "ETUDIANT"
                : dto.Role.Trim().ToUpper();

            string? photoPath = null;

            if (dto.Photo != null && dto.Photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "users"
                );

                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{dto.Photo.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Photo.CopyToAsync(stream);
                }

                photoPath = $"/uploads/users/{fileName}";
            }

            var user = new User
            {
                Nom = dto.Nom,
                Prenom = dto.Prenom,
                Matricule = dto.Matricule,
                Email = dto.Email,
                MotDePasseHash = dto.MotDePasse,
                PhotoPath = photoPath,
                Role = role,
                IsActive = false,
                DateCreation = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _db.DemandesInscription.Add(new DemandeInscription
            {
                UserId = user.Id,
                Statut = "EN_ATTENTE",
                Commentaire = $"Inscription {role}"
            });

            await _db.SaveChangesAsync();

            return Ok(new { message = "Compte créé, en attente de validation" });
        }

        private string GenerateJwt(int userId, string email, string role)
        {
            var key = _config["Jwt:Key"]!;
            var issuer = _config["Jwt:Issuer"]!;
            var audience = _config["Jwt:Audience"]!;

            var claims = new List<Claim>
            {
                new Claim("userId", userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}