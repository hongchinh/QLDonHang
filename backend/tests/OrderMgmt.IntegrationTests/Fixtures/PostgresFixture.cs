using Testcontainers.PostgreSql;
using Xunit;

namespace OrderMgmt.IntegrationTests.Fixtures;

/// <summary>
/// Spins up an ephemeral PostgreSQL instance for the test suite.
///
/// Default: Testcontainers (requires Docker Engine).
/// Override: set environment variable <c>TEST_DB_CONNECTION</c> to use an existing database
/// (the fixture will reuse it; the schema is rebuilt fresh in <see cref="WebAppFactory"/>).
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private const string ConnectionEnvVar = "TEST_DB_CONNECTION";

    private PostgreSqlContainer? _container;
    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable(ConnectionEnvVar);
        if (!string.IsNullOrWhiteSpace(external))
        {
            ConnectionString = external;
            return;
        }

        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("qldonhang_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
