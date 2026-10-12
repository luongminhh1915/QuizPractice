using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Customer;

/// <summary>Pay by QR Code and My Invoices (UC-18, UC-67; team extension, OI-09). The customer's own invoices only.</summary>
[Route("api/my/invoices")]
[Authorize(Policy = SettingValues.MenuItems.MyInvoices)]
public class MyInvoicesController(IMyInvoiceService invoices) : ApiControllerBase
{
    /// <summary>UC-18: 201 with the QR code; 409 Invoice.RegistrationNotPayable (E1) or Invoice.NoBankAccount (E2).</summary>
    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request, CancellationToken ct)
    {
        var result = await invoices.CreateAsync(request, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }

    /// <summary>Invoice QR (2.7): the QR code only while Unpaid.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Get(int id, CancellationToken ct) => FromResult(await invoices.GetAsync(id, ct));

    /// <summary>UC-18 A2: 409 Invoice.Closed when it is no longer Unpaid.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<InvoiceDto>> Cancel(int id, CancellationToken ct) => FromResult(await invoices.CancelAsync(id, ct));
}
