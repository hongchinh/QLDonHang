using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Models;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public class AuthTests : IAsyncLifetime
{
    private readonly PostgresFixture _pg;
    private WebAppFactory _factory = default!;
    private HttpClient _client = default!;

    public AuthTests(PostgresFixture pg) => _pg = pg;

    public async Task InitializeAsync()
    {
        _factory = new WebAppFactory(_pg.ConnectionString);
        await ((IAsyncLifetime)_factory).InitializeAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await ((IAsyncLifetime)_factory).DisposeAsync();
    }

    [Fact]
    public async Task Login_with_seeded_admin_returns_access_and_refresh_tokens()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "admin", Password = "Admin@123" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        body!.Success.Should().BeTrue();
        body.Data!.AccessToken.Should().NotBeNullOrEmpty();
        body.Data.RefreshToken.Should().NotBeNullOrEmpty();
        body.Data.User.Username.Should().Be("admin");
        body.Data.User.Roles.Should().Contain("ADMIN");
    }

    [Fact]
    public async Task Login_with_bad_password_returns_401_not_400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "admin", Password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        body!.Success.Should().BeFalse();
        body.Error!.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Refresh_rotates_token_and_revokes_old_one()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "admin", Password = "Admin@123" });
        var loginBody = await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var firstRefresh = loginBody!.Data!.RefreshToken;

        var rotate = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = firstRefresh });
        rotate.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotateBody = await rotate.Content.ReadFromJsonAsync<ApiResponse<TokenPairResponse>>();
        rotateBody!.Data!.RefreshToken.Should().NotBe(firstRefresh);

        // Reuse of the old token must be rejected.
        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = firstRefresh });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
