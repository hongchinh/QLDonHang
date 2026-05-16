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

        var afterCancel = await TransitionAsync(draft.Id, QuotationAction.Cancel);
        afterCancel.Status.Should().Be(QuotationStatus.Cancelled);
        afterCancel.CancelledAt.Should().NotBeNull();
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
}
