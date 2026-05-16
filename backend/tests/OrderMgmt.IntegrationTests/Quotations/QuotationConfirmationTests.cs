using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Enums;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class QuotationConfirmationTests : QuotationTestBase
{
    public QuotationConfirmationTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Confirm_RecordsConfirmedAtAndOwner()
    {
        var draft = await CreateAsync();
        await TransitionAsync(draft.Id, QuotationAction.Send);
        var confirmed = await TransitionAsync(draft.Id, QuotationAction.Confirm);

        confirmed.Status.Should().Be(QuotationStatus.Confirmed);
        confirmed.ConfirmedAt.Should().NotBeNull();
        confirmed.ConfirmedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        confirmed.ConfirmedByUserId.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminId = await db.Users.Where(u => u.Username == "admin").Select(u => u.Id).FirstAsync();
        confirmed.ConfirmedByUserId.Should().Be(adminId);
    }

    [Fact]
    public async Task Cancel_FromConfirmed_AsAdmin_SetsCancelledAt()
    {
        var draft = await CreateAsync();
        await TransitionAsync(draft.Id, QuotationAction.Send);
        await TransitionAsync(draft.Id, QuotationAction.Confirm);
        var cancelled = await TransitionAsync(draft.Id, QuotationAction.Cancel);

        cancelled.Status.Should().Be(QuotationStatus.Cancelled);
        cancelled.CancelledAt.Should().NotBeNull();
        cancelled.CancelledAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Cancel_FromConfirmed_WithoutPermission_Returns403()
    {
        var draft = await CreateAsync();
        await TransitionAsync(draft.Id, QuotationAction.Send);
        await TransitionAsync(draft.Id, QuotationAction.Confirm);

        await CreateTestUserAsync("sales-cancel", "Sales@123", RoleCodes.Sales);
        await AuthenticateAsync("sales-cancel", "Sales@123");

        var resp = await _client.PostAsJsonAsync(
            $"/api/quotations/{draft.Id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.Cancel });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<QuotationDto> CreateAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<QuotationDto> TransitionAsync(Guid id, QuotationAction action)
    {
        var resp = await _client.PostAsJsonAsync(
            $"/api/quotations/{id}/transition",
            new TransitionQuotationRequest { Action = action });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        return body!.Data!;
    }
}
