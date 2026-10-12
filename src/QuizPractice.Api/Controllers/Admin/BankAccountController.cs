using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Services.Admin;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Admin;

/// <summary>
/// Bank Account Settings (UC-70, OI-09): the one account every new invoice is paid to (BR-24).
/// Reached from Settings List, so it shares the Settings permission (Admin only).
/// </summary>
[Route("api/bank-account")]
[Authorize(Policy = SettingValues.MenuItems.Settings)]
public class BankAccountController(IBankAccountService account) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BankAccountDto>> Get(CancellationToken ct) =>
        FromResult(await account.GetAsync(ct));

    /// <summary>409 BankAccount.Changed (E2); 200 with changed false when nothing differed (A2).</summary>
    [HttpPut]
    public async Task<ActionResult<BankAccountSavedDto>> Save(SaveBankAccountRequest request, CancellationToken ct) =>
        FromResult(await account.SaveAsync(request, ct));

    /// <summary>Step 6: the sample QR text for the values on screen, saved or not.</summary>
    [HttpGet("sample-qr")]
    public async Task<ActionResult<SampleQrDto>> SampleQr([FromQuery] string? bankBin, [FromQuery] string? accountNumber, CancellationToken ct) =>
        FromResult(await account.SampleQrAsync(bankBin, accountNumber, ct));
}
