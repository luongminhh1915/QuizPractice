using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Customer;

/// <summary>CUS-03: UC-18 Pay by QR Code, UC-67 View My Invoices (MyInvoiceService).</summary>
public class MyInvoicesTests
{
    private readonly Mock<IInvoiceRepository> _invoices = new();
    private readonly Mock<IRegistrationRepository> _registrations = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly User _customer = TestData.Customer();
    private readonly Registration _java;
    private readonly Registration _sql;
    private Invoice? _added;

    public MyInvoicesTests()
    {
        _java = TestData.Registration(1, package: TestData.Package(100, salePrice: 300_000m), customer: _customer, createdById: null);
        _sql = TestData.Registration(2, package: TestData.Package(200, salePrice: 250_000m, subjectId: 2), customer: _customer, createdById: null);
        _registrations
            .Setup(r => r.ListOwnByIdsAsync(_customer.Id, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, IReadOnlyCollection<int> ids, CancellationToken _) =>
                new[] { _java, _sql }.Where(r => ids.Contains(r.Id)).ToList());
        _invoices.Setup(i => i.GetBankAccountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Bank());
        _invoices.Setup(i => i.Add(It.IsAny<Invoice>())).Callback<Invoice>(i => _added = i);
        _unitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private MyInvoiceService Service(Mock<ICurrentUser>? user = null) =>
        new(_invoices.Object, _registrations.Object, _unitOfWork.Object, (user ?? MockUser.Customer()).Object);

    private Invoice Own(Invoice invoice)
    {
        _invoices.Setup(i => i.GetOwnAsync(invoice.Id, _customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
        return invoice;
    }

    // ------------------------------------------------------------------ UC-18 create

    [Fact]
    public async Task CreateAsync_TwoSubmittedRegistrations_StoresUnpaidInvoiceWithQr()
    {
        var dto = ResultAssert.Succeeded(await Service().CreateAsync(new CreateInvoiceRequest([1, 2])));

        Assert.Equal(InvoiceStatus.Unpaid, dto.Status);
        Assert.Equal(550_000m, dto.TotalAmount);
        Assert.Matches("^QPS[0-9A-F]{6}$", dto.Code);
        Assert.Contains(dto.Code, dto.QrPayload);
        Assert.Equal("0000000000", dto.AccountNumber);
        Assert.Same(_added, _java.Invoice);
        Assert.Equal(2, _added!.Registrations.Count);
    }

    [Fact]
    public async Task CreateAsync_OneRegistration_IsAccepted()
    {
        var dto = ResultAssert.Succeeded(await Service().CreateAsync(new CreateInvoiceRequest([1])));

        Assert.Equal(300_000m, dto.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_CodeAlreadyUsed_DrawsAnotherCode()
    {
        _invoices.SetupSequence(i => i.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        ResultAssert.Succeeded(await Service().CreateAsync(new CreateInvoiceRequest([1])));

        _invoices.Verify(i => i.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_NoRegistrationChosen_ReturnsValidationError()
    {
        var result = await Service().CreateAsync(new CreateInvoiceRequest([]));

        ResultAssert.Invalid(result, "RegistrationIds");
    }

    [Fact]
    public async Task CreateAsync_TwentyOneRegistrations_ReturnsValidationError()
    {
        var result = await Service().CreateAsync(new CreateInvoiceRequest(Enumerable.Range(1, 21).ToList()));

        ResultAssert.Invalid(result, "RegistrationIds");
    }

    [Fact]
    public async Task CreateAsync_RegistrationOfAnotherCustomer_ReturnsNotFound()
    {
        var result = await Service().CreateAsync(new CreateInvoiceRequest([1, 99]));

        ResultAssert.Failed(result, ErrorType.NotFound, "Registration.NotFound");
        _invoices.Verify(i => i.Add(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PaidRegistration_ReturnsNotPayable()
    {
        _sql.Status = RegistrationStatus.Paid;

        var result = await Service().CreateAsync(new CreateInvoiceRequest([1, 2]));

        ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.RegistrationNotPayable");
    }

    [Fact]
    public async Task CreateAsync_RegistrationAlreadyInUnpaidInvoice_ReturnsNotPayable()
    {
        TestData.InvoiceWith(InvoiceStatus.Unpaid, _java);

        var result = await Service().CreateAsync(new CreateInvoiceRequest([1]));

        ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.RegistrationNotPayable");
    }

    [Fact]
    public async Task CreateAsync_NoReceivingBankAccount_ReturnsNoBankAccount()
    {
        _invoices.Setup(i => i.GetBankAccountAsync(It.IsAny<CancellationToken>())).ReturnsAsync((BankAccount?)null);

        var result = await Service().CreateAsync(new CreateInvoiceRequest([1]));

        ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.NoBankAccount");
        _invoices.Verify(i => i.Add(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RegistrationSettledMeanwhile_ReturnsNotPayable()
    {
        _unitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Service().CreateAsync(new CreateInvoiceRequest([1]));

        ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.RegistrationNotPayable");
    }

    [Fact]
    public async Task CreateAsync_Guest_ReturnsAuthRequired()
    {
        var result = await Service(MockUser.Guest()).CreateAsync(new CreateInvoiceRequest([1]));

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
    }
}
