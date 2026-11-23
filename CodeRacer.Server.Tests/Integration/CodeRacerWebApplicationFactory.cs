using CodeRacer.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace CodeRacer.Server.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Runs the real pipeline (DI, routing, JWT authentication) under the "Testing" environment
/// (appsettings.Testing.json) against an isolated PostgreSQL Testcontainer.
/// </summary>
public class CodeRacerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private readonly string _inMemoryDatabaseName = $"coderacer_test_{Guid.NewGuid():N}";
    private bool _useInMemoryFallback;

    public async Task InitializeAsync()
    {
        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("coderacer_test")
                .WithUsername("coderacer_test")
                .WithPassword("coderacer_test")
                .Build();
            await _postgres.StartAsync();
        }
        catch (Exception exception) when (!RequirePostgres())
        {
            _useInMemoryFallback = true;
            Console.WriteLine($"Docker is unavailable. Using EF Core InMemory for local integration tests: {exception.Message}");
            if (_postgres is not null)
            {
                await _postgres.DisposeAsync();
                _postgres = null;
            }
        }

        using IServiceScope scope = Services.CreateScope();
        CodeRacerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CodeRacerDbContext>();
        if (_useInMemoryFallback)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the application connection with the isolated Testcontainer connection.
            services.RemoveAll<DbContextOptions<CodeRacerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CodeRacerDbContext>>();

            if (_useInMemoryFallback)
            {
                services.AddDbContext<CodeRacerDbContext>(options =>
                    options.UseInMemoryDatabase(_inMemoryDatabaseName));
            }
            else
            {
                services.AddDbContext<CodeRacerDbContext>(options =>
                    options.UseNpgsql(_postgres!.GetConnectionString()));
            }
        });
    }

    private static bool RequirePostgres() =>
        string.Equals(
            Environment.GetEnvironmentVariable("CODE_RACER_REQUIRE_POSTGRES"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
