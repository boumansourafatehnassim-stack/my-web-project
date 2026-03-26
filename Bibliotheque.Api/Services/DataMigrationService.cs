using Bibliotheque.Api.Data;
using Bibliotheque.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bibliotheque.Api.Services
{
    public class DataMigrationService
    {
        private readonly SourceBibliothequeDbContext _source;
        private readonly BibliothequeDbContext _target;

        public DataMigrationService(
            SourceBibliothequeDbContext source,
            BibliothequeDbContext target)
        {
            _source = source;
            _target = target;
        }

        public async Task<string> MigrateAsync()
        {
            // مهم: ننقل بالترتيب
            await MigrateUsersAsync();
            await MigrateLivresAsync();
       
            await MigrateExemplairesAsync();
            await MigrateDemandesInscriptionAsync();
            await MigrateEmpruntsAsync();
            await MigrateDemandesEmpruntAsync();
            await MigrateNotificationsAsync();

            return "Migration terminée avec succès.";
        }

        private async Task MigrateUsersAsync()
        {
            if (await _target.Users.AnyAsync()) return;

            var items = await _source.Users
                .AsNoTracking()
                .ToListAsync();

            _target.Users.AddRange(items);
            await _target.SaveChangesAsync();
        }

        private async Task MigrateLivresAsync()
        {
            if (await _target.Livres.AnyAsync()) return;

            var items = await _source.Livres
                .AsNoTracking()
                .ToListAsync();

            _target.Livres.AddRange(items);
            await _target.SaveChangesAsync();
        }

     
        private async Task MigrateExemplairesAsync()
        {
            if (await _target.Exemplaires.AnyAsync()) return;

            var items = await _source.Exemplaires
                .AsNoTracking()
                .ToListAsync();

            _target.Exemplaires.AddRange(items);
            await _target.SaveChangesAsync();
        }

        private async Task MigrateDemandesInscriptionAsync()
        {
            if (await _target.DemandesInscription.AnyAsync()) return;

            var items = await _source.DemandesInscription
                .AsNoTracking()
                .ToListAsync();

            _target.DemandesInscription.AddRange(items);
            await _target.SaveChangesAsync();
        }

        private async Task MigrateEmpruntsAsync()
        {
            if (await _target.Emprunts.AnyAsync()) return;

            var items = await _source.Emprunts
                .AsNoTracking()
                .ToListAsync();

            _target.Emprunts.AddRange(items);
            await _target.SaveChangesAsync();
        }

        private async Task MigrateDemandesEmpruntAsync()
        {
            if (await _target.DemandesEmprunt.AnyAsync()) return;

            var items = await _source.DemandesEmprunt
                .AsNoTracking()
                .ToListAsync();

            _target.DemandesEmprunt.AddRange(items);
            await _target.SaveChangesAsync();
        }

        private async Task MigrateNotificationsAsync()
        {
            if (await _target.Notifications.AnyAsync()) return;

            var items = await _source.Notifications
                .AsNoTracking()
                .ToListAsync();

            _target.Notifications.AddRange(items);
            await _target.SaveChangesAsync();
        }
    }
}