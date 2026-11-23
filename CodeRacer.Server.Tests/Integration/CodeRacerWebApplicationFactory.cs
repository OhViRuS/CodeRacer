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
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("coderacer_test")
        .WithUsername("coderacer_test")
        .WithPassword("coderacer_test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using IServiceScope scope = Services.CreateScope();
        CodeRacerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CodeRacerDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the application connection with the isolated Testcontainer connection.
            services.RemoveAll<DbContextOptions<CodeRacerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CodeRacerDbContext>>();

            services.AddDbContext<CodeRacerDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}
