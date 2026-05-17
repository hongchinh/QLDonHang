using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Admin.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Entities.Identity;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using OrderMgmt.IntegrationTests.Quotations;
using Xunit;

namespace OrderMgmt.IntegrationTests.Admin;

[Collection(nameof(PostgresCollection))]
public class AdminRolesCrudTests : QuotationTestBase
{
    public AdminRolesCrudTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task List_returns_5_system_roles_with_user_and_permission_count()
    {
        var res = await _client.GetAsync("/api/admin/roles");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<RoleListItemDto>>>(TestJson.Options);
        body!.Data!.Should().Contain(r => r.Code == RoleCodes.Admin && r.IsSystem);
        body.Data.Should().Contain(r => r.Code == RoleCodes.Sales && r.IsSystem);
        body.Data.Should().Contain(r => r.Code == RoleCodes.Accountant && r.IsSystem);
        body.Data.Should().Contain(r => r.Code == RoleCodes.Warehouse && r.IsSystem);
        body.Data.Should().Contain(r => r.Code == RoleCodes.Manager && r.IsSystem);

        var admin = body.Data.First(r => r.Code == RoleCodes.Admin);
        admin.PermissionCount.Should().BeGreaterThan(0);
        admin.UserCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Get_returns_role_with_permission_codes()
    {
        var listRes = await _client.GetFromJsonAsync<ApiResponse<IReadOnlyList<RoleListItemDto>>>(
            "/api/admin/roles", TestJson.Options);
        var sales = listRes!.Data!.First(r => r.Code == RoleCodes.Sales);

        var res = await _client.GetAsync($"/api/admin/roles/{sales.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<RoleDetailDto>>(TestJson.Options);
        body!.Data!.Code.Should().Be(RoleCodes.Sales);
        body.Data.PermissionCodes.Should().Contain(Permissions.Quotations.View);
        body.Data.PermissionCodes.Should().Contain(Permissions.Customers.View);
        body.Data.PermissionCodes.Should().NotContain(Permissions.Quotations.Delete);
    }

    [Fact]
    public async Task Create_custom_role_succeeds_and_isSystem_false()
    {
        var payload = new CreateRoleRequest
        {
            Code = "TEST_CREATE_OK",
            Name = "Test create OK",
            Description = "ut",
            PermissionCodes = new[] { Permissions.Quotations.View, Permissions.Customers.View },
        };
        var res = await _client.PostAsJsonAsync("/api/admin/roles", payload);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<RoleDetailDto>>(TestJson.Options);
        body!.Data!.Code.Should().Be("TEST_CREATE_OK");
        body.Data.IsSystem.Should().BeFalse();
        body.Data.PermissionCodes.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_with_duplicate_code_returns_409()
    {
        await CreateCustomRoleAsync("TEST_DUP_CODE", "Dup code original");

        var payload = new CreateRoleRequest
        {
            Code = "TEST_DUP_CODE",
            Name = "Dup code attempt",
            PermissionCodes = Array.Empty<string>(),
        };
        var res = await _client.PostAsJsonAsync("/api/admin/roles", payload);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse>(TestJson.Options);
        body!.Error!.Code.Should().Be("CONFLICT");
    }

    [Fact]
    public async Task Create_with_duplicate_name_returns_409()
    {
        await CreateCustomRoleAsync("TEST_DUP_NAME_A", "Trưởng nhóm bán hàng");

        var payload = new CreateRoleRequest
        {
            Code = "TEST_DUP_NAME_B",
            Name = "TRUONG NHOM BAN HANG", // case + accent insensitive should match
            PermissionCodes = Array.Empty<string>(),
        };
        var res = await _client.PostAsJsonAsync("/api/admin/roles", payload);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("SALES")]
    [InlineData("admin")] // lowercase still reserved
    [InlineData("Accountant")]
    public async Task Create_with_reserved_system_code_returns_400(string reservedCode)
    {
        var payload = new CreateRoleRequest
        {
            Code = reservedCode,
            Name = $"Try {reservedCode}",
            PermissionCodes = Array.Empty<string>(),
        };
        var res = await _client.PostAsJsonAsync("/api/admin/roles", payload);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("sales")]   // lowercase
    [InlineData("1ABC")]    // starts with digit
    [InlineData("A")]       // too short
    public async Task Create_with_invalid_code_format_returns_400(string badCode)
    {
        var payload = new CreateRoleRequest
        {
            Code = badCode,
            Name = "Bad code",
            PermissionCodes = Array.Empty<string>(),
        };
        var res = await _client.PostAsJsonAsync("/api/admin/roles", payload);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_unknown_permission_code_returns_409()
    {
        var payload = new CreateRoleRequest
        {
            Code = "TEST_BAD_PERM",
            Name = "Bad perm",
            PermissionCodes = new[] { "nonexistent.permission" },
        };
        var res = await _client.PostAsJsonAsync("/api/admin/roles", payload);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_custom_role_name_succeeds()
    {
        var created = await CreateCustomRoleAsync("TEST_RENAME", "Original name");

        var update = new UpdateRoleRequest { Name = "Renamed", Description = "updated" };
        var res = await _client.PutAsJsonAsync($"/api/admin/roles/{created.Id}", update);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<RoleDetailDto>>(TestJson.Options);
        body!.Data!.Name.Should().Be("Renamed");
        body.Data.Description.Should().Be("updated");
    }

    [Fact]
    public async Task Update_system_role_name_returns_403()
    {
        var sales = await GetRoleByCodeAsync(RoleCodes.Sales);

        var update = new UpdateRoleRequest { Name = "Hacker rename" };
        var res = await _client.PutAsJsonAsync($"/api/admin/roles/{sales.Id}", update);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_custom_role_without_users_succeeds()
    {
        var created = await CreateCustomRoleAsync("TEST_DEL_OK", "Delete me");

        var res = await _client.DeleteAsync($"/api/admin/roles/{created.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var still = await db.Roles
            .IgnoreQueryFilters()
            .FirstAsync(r => r.Id == created.Id);
        still.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_custom_role_with_users_returns_409()
    {
        var created = await CreateCustomRoleAsync("TEST_DEL_WITH_USER", "Has users");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<OrderMgmt.Application.Identity.Interfaces.IPasswordHasher>();
            db.Users.Add(new User
            {
                Username = "ut_role_user",
                Email = "ut_role_user@test.local",
                FullName = "Role user",
                PasswordHash = hasher.Hash("Pass@123"),
                Status = OrderMgmt.Domain.Enums.UserStatus.Active,
                UserRoles = new List<UserRole> { new() { RoleId = created.Id } },
            });
            await db.SaveChangesAsync();
        }

        var res = await _client.DeleteAsync($"/api/admin/roles/{created.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse>(TestJson.Options);
        body!.Error!.Message.Should().Contain("user");
    }

    [Fact]
    public async Task Delete_system_role_returns_403()
    {
        var sales = await GetRoleByCodeAsync(RoleCodes.Sales);
        var res = await _client.DeleteAsync($"/api/admin/roles/{sales.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_role_hard_removes_role_permission_rows()
    {
        // RolePermission is a pure join entity (no ISoftDeletable), so DeleteAsync hard-deletes
        // its rows before soft-deleting the role itself. Verify both effects.
        var created = await CreateCustomRoleAsync(
            "TEST_CASCADE", "Cascade target",
            permissionCodes: new[] { Permissions.Quotations.View, Permissions.Customers.View });

        // Pre-condition: rows exist.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.RolePermissions.CountAsync(rp => rp.RoleId == created.Id))
                .Should().Be(2);
        }

        var res = await _client.DeleteAsync($"/api/admin/roles/{created.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            // RolePermission rows are gone (hard-delete).
            (await db.RolePermissions.CountAsync(rp => rp.RoleId == created.Id))
                .Should().Be(0);
            // Role itself is soft-deleted (IsDeleted = true, still present).
            var role = await db.Roles
                .IgnoreQueryFilters()
                .FirstAsync(r => r.Id == created.Id);
            role.IsDeleted.Should().BeTrue();
        }
    }

    private async Task<RoleDetailDto> CreateCustomRoleAsync(
        string code, string name, string[]? permissionCodes = null)
    {
        var payload = new CreateRoleRequest
        {
            Code = code,
            Name = name,
            PermissionCodes = permissionCodes ?? Array.Empty<string>(),
        };
        var res = await _client.PostAsJsonAsync("/api/admin/roles", payload);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<ApiResponse<RoleDetailDto>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<RoleListItemDto> GetRoleByCodeAsync(string code)
    {
        var listRes = await _client.GetFromJsonAsync<ApiResponse<IReadOnlyList<RoleListItemDto>>>(
            "/api/admin/roles", TestJson.Options);
        return listRes!.Data!.First(r => r.Code == code);
    }
}
