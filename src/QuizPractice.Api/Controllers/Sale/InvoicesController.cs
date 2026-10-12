using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Application.Services.Sale;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Sale;

/// <summary>Invoices List and Invoice Details (UC-68, UC-69; team extension, OI-09).</summary>
[Route("api/invoices")]
[Authorize(Policy = SettingValues.MenuItems.Invoices)]
public class InvoicesController(IInvoiceService invoices) : ApiControllerBase
{
    /// <summary>UC-68: ?search=&amp;status=Unpaid|Paid|Cancelled&amp;from=&amp;to=&amp;page=.</summary>
    [HttpGet]
    public async Task<ActionResult<Page<SaleInvoiceSummaryDto>>> List([FromQuery] InvoiceListRequest request, CancellationToken ct) =>
        FromResult(await invoices.ListAsync(request, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleInvoiceDetailsDto>> Get(int id, CancellationToken ct) => FromResult(await invoices.GetAsync(id, ct));

    /// <summary>UC-69: 409 Invoice.Cancelled (E1), Invoice.Paid (E3), Registration.NoDuration (E2) or Invoice.Changed.</summary>
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<SaleInvoiceDetailsDto>> Confirm(int id, InvoiceNotesRequest request, CancellationToken ct) =>
        FromResult(await invoices.ConfirmAsync(id, request, ct));

    /// <summary>UC-69 A1–A3: the notes alone.</summary>
    [HttpPut("{id:int}/notes")]
    public async Task<ActionResult<SaleInvoiceDetailsDto>> SaveNotes(int id, InvoiceNotesRequest request, CancellationToken ct) =>
        FromResult(await invoices.SaveNotesAsync(id, request, ct));
}
