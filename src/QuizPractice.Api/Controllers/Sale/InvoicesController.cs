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
}
