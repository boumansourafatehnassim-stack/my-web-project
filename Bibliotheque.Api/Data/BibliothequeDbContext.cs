using System;
using System.Data;
using System.Threading.Tasks;
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
        public DbSet<Exemplaire> Exemplaires { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Emprunt> Emprunts { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<DemandeInscription> DemandesInscription { get; set; } = null!;
        public DbSet<DemandeEmprunt> DemandesEmprunt { get; set; } = null!;

        // Stored Procedures Results (Keyless)
        public DbSet<DemandeEmpruntResult> DemandeEmpruntResults { get; set; } = null!;
        public DbSet<TraiterDemandeResult> TraiterDemandeResults { get; set; } = null!;
        public DbSet<RetourResult> RetourResults { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // LIVRES
            // =========================
            modelBuilder.Entity<Livre>(e =>
            {
                e.ToTable("Livres");

                e.HasKey(x => x.Id);

                e.Property(x => x.Titre)
                    .IsRequired()
                    .HasMaxLength(500);

                e.Property(x => x.Auteur)
                    .IsRequired()
                    .HasMaxLength(300);

                e.Property(x => x.Theme)
                    .HasMaxLength(200);

                e.Property(x => x.Langue)
                    .HasMaxLength(100);

                e.Property(x => x.AdresseBibliogr)
                    .HasMaxLength(1000);

                e.Property(x => x.IsDeleted)
                    .HasDefaultValue(false);

                e.HasMany(x => x.Exemplaires)
                    .WithOne(x => x.Livre)
                    .HasForeignKey(x => x.LivreId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================
            // EXEMPLAIRES
            // =========================
            modelBuilder.Entity<Exemplaire>(e =>
            {
                e.ToTable("Exemplaires");

                e.HasKey(x => x.Id);

                e.Property(x => x.CodeBarres)
                    .IsRequired()
                    .HasMaxLength(100);

                e.Property(x => x.Statut)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("DISPONIBLE");

                e.Property(x => x.Emplacement)
                    .HasMaxLength(200);

                e.HasIndex(x => x.CodeBarres)
                    .IsUnique();
            });

            // =========================
            // DEMANDES EMPRUNT
            // =========================
            modelBuilder.Entity<DemandeEmprunt>(e =>
            {
                e.ToTable("DemandesEmprunt");
                e.HasKey(x => x.Id);
            });

            // =========================
            // KEYLESS SP RESULTS
            // =========================
            modelBuilder.Entity<DemandeEmpruntResult>(e =>
            {
                e.HasNoKey();
                e.ToView(null);
            });

            modelBuilder.Entity<TraiterDemandeResult>(e =>
            {
                e.HasNoKey();
                e.ToView(null);
            });

            modelBuilder.Entity<RetourResult>(e =>
            {
                e.HasNoKey();
                e.ToView(null);
            });
        }

        // =========================
        // STORED PROCEDURES
        // =========================

        public async Task<DemandeEmpruntResult> DemanderEmpruntAsync(int userId, int livreId, string? commentaire)
        {
            var pUserId = new SqlParameter("@UserId", userId);
            var pLivreId = new SqlParameter("@LivreId", livreId);
            var pCommentaire = new SqlParameter("@Commentaire", (object?)commentaire ?? DBNull.Value);

            var list = await DemandeEmpruntResults
                .FromSqlRaw("EXEC sp_DemanderEmprunt @UserId, @LivreId, @Commentaire", pUserId, pLivreId, pCommentaire)
                .AsNoTracking()
                .ToListAsync();

            if (list.Count == 0)
                throw new Exception("sp_DemanderEmprunt ما رجعت حتى نتيجة");

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

            if (list.Count == 0)
                throw new Exception("sp_TraiterDemande ما رجعت حتى نتيجة");

            return list[0];
        }

        public async Task<RetourResult> EnregistrerRetourAsync(int empruntId)
        {
            var pEmpruntId = new SqlParameter("@EmpruntId", empruntId);

            var list = await RetourResults
                .FromSqlRaw("EXEC sp_EnregistrerRetour @EmpruntId", pEmpruntId)
                .AsNoTracking()
                .ToListAsync();

            if (list.Count == 0)
                throw new Exception("sp_EnregistrerRetour ما رجعت حتى نتيجة");

            return list[0];
        }
    }
}