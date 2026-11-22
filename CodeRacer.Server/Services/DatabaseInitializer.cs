using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Services;

/// <summary>
/// Runs once at startup. Pending EF Core migrations are applied first, so a fresh or outdated database
/// no longer crashes the app with "relation does not exist", and then the starter snippets are seeded.
/// </summary>
public class DatabaseInitializer(CodeRacerDbContext context, ISnippetSeeder seeder) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Migrations only exist for a relational database; the in-memory provider used by the
        // integration tests has no schema to migrate.
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        await seeder.SeedAsync(cancellationToken);
    }
}
