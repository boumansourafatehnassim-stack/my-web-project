using Bibliotheque.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Data
{
    public class SourceBibliothequeDbContext : DbContext
    {
        public SourceBibliothequeDbContext(DbContextOptions<SourceBibliothequeDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Livre> Livres => Set<Livre>();
        public DbSet<Exemplaire> Exemplaires => Set<Exemplaire>();
        public DbSet<Emprunt> Emprunts => Set<Emprunt>();
        public DbSet<DemandeEmprunt> DemandesEmprunt => Set<DemandeEmprunt>();
        public DbSet<DemandeInscription> DemandesInscription => Set<DemandeInscription>();
        public DbSet<Notification> Notifications => Set<Notification>();
     

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Livre>().ToTable("Livres");
            modelBuilder.Entity<Exemplaire>().ToTable("Exemplaires");
            modelBuilder.Entity<Emprunt>().ToTable("Emprunts");
            modelBuilder.Entity<DemandeEmprunt>().ToTable("DemandesEmprunt");
            modelBuilder.Entity<DemandeInscription>().ToTable("DemandesInscription");
            modelBuilder.Entity<Notification>().ToTable("Notifications");
          
        }
    }
}