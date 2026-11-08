using CodeRacer.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeRacer.Server.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Runs the real pipeline (DI, routing, JWT authentication) under the "Testing" environment
/// (appsettings.Testing.json) but swaps PostgreSQL for an isolated EF Core in-memory database,
/// so the tests need no running database server or credentials.
/// </summary>
public class CodeRacerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "CodeRacerTests_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql registration from Program.cs; registering a second provider
            // on top of it would make EF Core refuse to build the context.
            services.RemoveAll<DbContextOptions<CodeRacerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CodeRacerDbContext>>();

            services.AddDbContext<CodeRacerDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
