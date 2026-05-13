# Phase 05 — Quotation CRUD controller

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** S

## Objective
Expose the Quotation use cases through HTTP. CRUD + transition only; the `/pdf` endpoint is added in Phase 06 once the renderer exists. Permission attributes match the seeded permission codes.

## Files
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs` (new)

## Tasks
1. Create controller mirroring the shape of `ProductsController` (validators injected as `IValidator<T>` parameters):
   ```csharp
   public class QuotationsController : ApiControllerBase
   {
       private readonly IQuotationService _quotations;
       private readonly IValidator<UpsertQuotationRequest> _upsertValidator;
       private readonly IValidator<QuotationListRequest> _listValidator;
       private readonly IValidator<TransitionQuotationRequest> _transitionValidator;

       public QuotationsController(
           IQuotationService quotations,
           IValidator<UpsertQuotationRequest> upsertValidator,
           IValidator<QuotationListRequest> listValidator,
           IValidator<TransitionQuotationRequest> transitionValidator)
       { /* assign */ }

       [HttpGet]
       [HasPermission(Permissions.Quotations.View)]
       public async Task<ActionResult<ApiResponse<PagedResult<QuotationListItemDto>>>> List(
           [FromQuery] QuotationListRequest request, CancellationToken ct)
       {
           await _listValidator.ValidateAndThrowAsync(request, ct);
           return Success(await _quotations.ListAsync(request, ct));
       }

       [HttpGet("{id:guid}")]
       [HasPermission(Permissions.Quotations.View)]
       public async Task<ActionResult<ApiResponse<QuotationDto>>> Get(Guid id, CancellationToken ct)
           => Success(await _quotations.GetAsync(id, ct));

       [HttpPost]
       [HasPermission(Permissions.Quotations.Create)]
       public async Task<ActionResult<ApiResponse<QuotationDto>>> Create(
           [FromBody] UpsertQuotationRequest request, CancellationToken ct)
       {
           await _upsertValidator.ValidateAndThrowAsync(request, ct);
           return Success(await _quotations.CreateAsync(request, ct));
       }

       [HttpPut("{id:guid}")]
       [HasPermission(Permissions.Quotations.Update)]
       public async Task<ActionResult<ApiResponse<QuotationDto>>> Update(
           Guid id, [FromBody] UpsertQuotationRequest request, CancellationToken ct)
       {
           await _upsertValidator.ValidateAndThrowAsync(request, ct);
           return Success(await _quotations.UpdateAsync(id, request, ct));
       }

       [HttpDelete("{id:guid}")]
       [HasPermission(Permissions.Quotations.Delete)]
       public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
       {
           await _quotations.DeleteAsync(id, ct);
           return Success();
       }

       [HttpPost("{id:guid}/transition")]
       [HasPermission(Permissions.Quotations.Update)]
       public async Task<ActionResult<ApiResponse<QuotationDto>>> Transition(
           Guid id, [FromBody] TransitionQuotationRequest request, CancellationToken ct)
       {
           await _transitionValidator.ValidateAndThrowAsync(request, ct);
           return Success(await _quotations.TransitionAsync(id, request.Action, ct));
       }
   }
   ```
2. No additional DI wiring needed — `AddValidatorsFromAssembly` already picks the validators up.

## Verification
```
# Building WebApi while it's running fails due to file locks per saved memory.
# Build only Application to confirm symbols resolve — controller is part of WebApi which the user restarts.
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
```

After user restarts WebApi: Swagger should list 5 quotation endpoints + `/transition`.

## Exit Criteria
- Controller file present, all 6 actions implemented.
- Each protected by the correct `[HasPermission(Permissions.Quotations.X)]`.
- The route inherits `api/[controller]` from `ApiControllerBase`, so paths are `/api/quotations` and `/api/quotations/{id}` and `/api/quotations/{id}/transition`.
