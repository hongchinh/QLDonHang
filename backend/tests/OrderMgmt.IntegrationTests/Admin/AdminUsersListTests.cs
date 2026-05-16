using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Admin.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Enums;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using OrderMgmt.IntegrationTests.Quotations;
using Xunit;

namespace OrderMgmt.IntegrationTests.Admin;

[Collection(nameof(PostgresCollection))]
public class AdminUsersListTests : QuotationTestBase
{
    public AdminUsersListTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Admin_can_list_users_returns_admin_plus_test_users()
    {
        await CreateTestUserAsync("sales1", "Sales@123", RoleCodes.Sales);

        var response = await _client.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminUserListItemDto>>>(TestJson.Options);
        body!.Data!.Should().Contain(u => u.Username == "admin");
        body.Data!.Should().Contain(u => u.Username == "sales1" && u.RoleCode == RoleCodes.Sales);
    }

    [Fact]
    public async Task Sales_user_gets_forbidden()
    {
        await CreateTestUserAsync("sales_forbidden", "Sales@123", RoleCodes.Sales);
        await AuthenticateAsync("sales_forbidden", "Sales@123");

        var response = await _client.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActiveOnly_filter_excludes_disabled_user()
    {
        await CreateTestUserAsync("sales_active", "Sales@123", RoleCodes.Sales);
        await CreateTestUserAsync("sales_disabled", "Sales@123", RoleCodes.Sales);
        await SetUserStatusAsync("sales_disabled", UserStatus.Disabled);

        var response = await _client.GetAsync("/api/admin/users?activeOnly=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminUserListItemDto>>>(TestJson.Options);
        body!.Data!.Should().Contain(u => u.Username == "sales_active");
        body.Data!.Should().NotContain(u => u.Username == "sales_disabled");
    }

    [Fact]
    public async Task Search_filters_by_username_substring()
    {
        await CreateTestUserAsync("alpha_user", "Sales@123", RoleCodes.Sales);
        await CreateTestUserAsync("beta_user", "Sales@123", RoleCodes.Sales);

        var response = await _client.GetAsync("/api/admin/users?search=alpha");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminUserListItemDto>>>(TestJson.Options);
        body!.Data!.Should().Contain(u => u.Username == "alpha_user");
        body.Data!.Should().NotContain(u => u.Username == "beta_user");
    }

    [Fact]
    public async Task Includes_soft_deleted_user_when_activeOnly_false()
    {
        await CreateTestUserAsync("sales_softdel", "Sales@123", RoleCodes.Sales);
        await SoftDeleteUserAsync("sales_softdel");

        var response = await _client.GetAsync("/api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminUserListItemDto>>>(TestJson.Options);
        var soft = body!.Data!.FirstOrDefault(u => u.Username == "sales_softdel");
        soft.Should().NotBeNull();
        soft!.IsActive.Should().BeFalse();
    }

    private async Task SetUserStatusAsync(string username, UserStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Username == username);
        user.Status = status;
        await db.SaveChangesAsync();
    }

    private async Task SoftDeleteUserAsync(string username)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Username == username);
        user.IsDeleted = true;
        await db.SaveChangesAsync();
    }
}
