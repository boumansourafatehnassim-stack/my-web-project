using System.Data;
using Bibliotheque.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Data
{
    public class BibliothequeDbContext : DbContext
    {
        public BibliothequeDbContext(DbContextOptions<BibliothequeDbContext> options)
            : base(options) { }

        // Tables
        public DbSet<Livre> Livres { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Emprunt> Emprunts { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<DemandeInscription> DemandesInscription { get; set; } = null!;
        public DbSet<Exemplaire> Exemplaires { get; set; } = null!;
        // ✅ REAL TABLE DemandesEmprunt
        public DbSet<DemandeEmprunt> DemandesEmprunt { get; set; } = null!;

        // Stored Procedures Results (Keyless)
        public DbSet<DemandeEmpruntResult> DemandeEmpruntResults { get; set; } = null!;
        public DbSet<TraiterDemandeResult> TraiterDemandeResults { get; set; } = null!;
        public DbSet<RetourResult> RetourResults { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Map table explicitly (هذا هو اللي يقتل 80% من المشاكل)
            modelBuilder.Entity<DemandeEmprunt>(e =>
            {
                e.ToTable("DemandesEmprunt"); // اسم الجدول بالضبط
                e.HasKey(x => x.Id);
            });

            // Keyless SP Results
            modelBuilder.Entity<DemandeEmpruntResult>(e => { e.HasNoKey(); e.ToView(null); });
            modelBuilder.Entity<TraiterDemandeResult>(e => { e.HasNoKey(); e.ToView(null); });
            modelBuilder.Entity<RetourResult>(e => { e.HasNoKey(); e.ToView(null); });
        }

        // Stored procedures calls
        public async Task<DemandeEmpruntResult> DemanderEmpruntAsync(int userId, int livreId, string? commentaire)
        {
            var pUserId = new SqlParameter("@UserId", userId);
            var pLivreId = new SqlParameter("@LivreId", livreId);
            var pCommentaire = new SqlParameter("@Commentaire", (object?)commentaire ?? DBNull.Value);

            var list = await DemandeEmpruntResults
                .FromSqlRaw("EXEC sp_DemanderEmprunt @UserId, @LivreId, @Commentaire", pUserId, pLivreId, pCommentaire)
                .AsNoTracking()
                .ToListAsync();

            if (list.Count == 0) throw new Exception("sp_DemanderEmprunt ما رجعت حتى نتيجة");
            return list[0];
        }

        public async Task<TraiterDemandeResult> TraiterDemandeAsync(int demandeId, string action)
        {
            var pDemandeId = new SqlParameter("@DemandeId", demandeId);
            var pAction = new SqlParameter("@Action", action);

            var list = await TraiterDemandeResults
                .FromSqlRaw("EXEC sp_TraiterDemande @DemandeId, @Action", pDemandeId, pAction)
                .AsNoTracking()
                .ToListAsync();

            if (list.Count == 0) throw new Exception("sp_TraiterDemande ما رجعت حتى نتيجة");
            return list[0];
        }

        public async Task<RetourResult> EnregistrerRetourAsync(int empruntId)
        {
            var pEmpruntId = new SqlParameter("@EmpruntId", empruntId);

            var list = await RetourResults
                .FromSqlRaw("EXEC sp_EnregistrerRetour @EmpruntId", pEmpruntId)
                .AsNoTracking()
                .ToListAsync();

            if (list.Count == 0) throw new Exception("sp_EnregistrerRetour ما رجعت حتى نتيجة");
            return list[0];
        }
    }
}
