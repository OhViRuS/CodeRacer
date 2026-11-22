using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Tests.Unit;

public class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_NonRelationalProvider_SkipsMigrationsAndStillSeeds()
    {
        using var context = new CodeRacerDbContext(
            new DbContextOptionsBuilder<CodeRacerDbContext>()
                .UseInMemoryDatabase("InitializerTests_" + Guid.NewGuid())
                .Options);
        var seeder = new RecordingSeeder();

        await new DatabaseInitializer(context, seeder).InitializeAsync(CancellationToken.None);

        Assert.Equal(1, seeder.Calls);
    }

    private sealed class RecordingSeeder : ISnippetSeeder
    {
        public int Calls { get; private set; }

        public Task<int> SeedAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(0);
        }
    }
}
