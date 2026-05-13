using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.Infrastructure.Persistence.Seed;
using Xunit;

namespace OrderMgmt.IntegrationTests.Fixtures;

public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString;

    public WebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["Jwt:Secret"] = "test-secret-do-not-use-in-production-min-32-chars-bbbbbbbbbbbbb",
                ["Jwt:Issuer"] = "OrderMgmtTest",
                ["Jwt:Audience"] = "OrderMgmtTestClient",
                ["Jwt:ExpiresInMinutes"] = "60",
                ["RefreshToken:ExpiresInDays"] = "14",
                ["Database:AutoMigrateAndSeed"] = "false",
                ["Seed:AdminPassword"] = "Admin@123",
            });
        });

        builder.ConfigureServices(services =>
        {
            // No-op: Infrastructure already wires Npgsql via configuration.
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(Services);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }
}
