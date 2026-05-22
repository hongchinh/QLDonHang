using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class QuotationStateMachineTests : QuotationTestBase
{
    public QuotationStateMachineTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Allowed_transitions_progress_status()
    {
        var draft = await CreateDraftAsync();
        var afterSend = await TransitionAsync(draft.Id, QuotationAction.Send);
        afterSend.Status.Should().Be(QuotationStatus.Sent);

        var afterConfirm = await TransitionAsync(draft.Id, QuotationAction.Confirm);
        afterConfirm.Status.Should().Be(QuotationStatus.Confirmed);
        afterConfirm.ConfirmedAt.Should().NotBeNull();
        afterConfirm.ConfirmedByUserId.Should().NotBeNull();

        var afterAccountingConfirm = await TransitionAsync(draft.Id, QuotationAction.AccountingConfirm);
        afterAccountingConfirm.Status.Should().Be(QuotationStatus.AccountingConfirmed);
        afterAccountingConfirm.AccountingConfirmedAt.Should().NotBeNull();
        afterAccountingConfirm.AccountingConfirmedByUserId.Should().NotBeNull();

        var afterCancel = await TransitionAsync(draft.Id, QuotationAction.Cancel);
        afterCancel.Status.Should().Be(QuotationStatus.Cancelled);
        afterCancel.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Confirmed_to_AccountingConfirmed_succeeds()
    {
        var draft = await CreateDraftAsync();
        await TransitionToConfirmedAsync(draft.Id);

        var result = await TransitionAsync(draft.Id, QuotationAction.AccountingConfirm);

        result.Status.Should().Be(QuotationStatus.AccountingConfirmed);
        result.AccountingConfirmedAt.Should().NotBeNull();
        result.AccountingConfirmedByUserId.Should().NotBeNull();
        result.AccountingConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task AccountingConfirmed_to_Cancelled_succeeds()
    {
        var draft = await CreateDraftAsync();
        await TransitionToConfirmedAsync(draft.Id);
        await TransitionAsync(draft.Id, QuotationAction.AccountingConfirm);

        var result = await TransitionAsync(draft.Id, QuotationAction.Cancel);

        result.Status.Should().Be(QuotationStatus.Cancelled);
        result.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Cannot_accounting_confirm_from_draft()
    {
        var draft = await CreateDraftAsync();
        var resp = await _client.PostAsJsonAsync(
            $"/api/quotations/{draft.Id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.AccountingConfirm });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cannot_accounting_confirm_from_sent()
    {
        var draft = await CreateDraftAsync();
        await TransitionAsync(draft.Id, QuotationAction.Send);
        var resp = await _client.PostAsJsonAsync(
            $"/api/quotations/{draft.Id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.AccountingConfirm });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cannot_confirm_directly_from_draft()
    {
        var draft = await CreateDraftAsync();
        var resp = await _client.PostAsJsonAsync(
            $"/api/quotations/{draft.Id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.Confirm });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cannot_uncancel()
    {
        var draft = await CreateDraftAsync();
        await TransitionAsync(draft.Id, QuotationAction.Cancel);

        var resp = await _client.PostAsJsonAsync(
            $"/api/quotations/{draft.Id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.Send });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_on_cancelled_returns_conflict()
    {
        var draft = await CreateDraftAsync();
        await TransitionAsync(draft.Id, QuotationAction.Cancel);

        var resp = await _client.PutAsJsonAsync($"/api/quotations/{draft.Id}", BuildRequest());
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse>(TestJson.Options);
        body!.Error!.Message.Should().Contain("đã hủy");
    }

    private async Task<QuotationDto> CreateDraftAsync()
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

    private async Task<QuotationDto> TransitionToConfirmedAsync(Guid id)
    {
        await TransitionAsync(id, QuotationAction.Send);
        return await TransitionAsync(id, QuotationAction.Confirm);
    }
}
