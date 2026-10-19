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

    // ------------------------------------------------------------------ UC-18 view and cancel

    [Fact]
    public async Task GetAsync_UnpaidInvoice_HasQrCode()
    {
        Own(TestData.InvoiceWith(InvoiceStatus.Unpaid, _java));

        var dto = ResultAssert.Succeeded(await Service().GetAsync(50));

        Assert.NotNull(dto.QrPayload);
    }

    [Fact]
    public async Task GetAsync_PaidInvoice_HasNoQrCode()
    {
        Own(TestData.InvoiceWith(InvoiceStatus.Paid, _java));

        var dto = ResultAssert.Succeeded(await Service().GetAsync(50));

        Assert.Null(dto.QrPayload);
    }

    [Fact]
    public async Task CancelAsync_UnpaidInvoice_BecomesCancelledAndFreesItsRegistrations()
    {
        var invoice = Own(TestData.InvoiceWith(InvoiceStatus.Unpaid, _java));

        var dto = ResultAssert.Succeeded(await Service().CancelAsync(50));

        Assert.Equal(InvoiceStatus.Cancelled, dto.Status);
        Assert.Null(dto.QrPayload);
        Assert.True(MyRegistrationDto.From(_java).CanChange);
        Assert.Equal(RegistrationStatus.Submitted, invoice.Registrations[0].Status);
    }

    [Fact]
    public async Task CancelAsync_PaidInvoice_ReturnsClosed()
    {
        Own(TestData.InvoiceWith(InvoiceStatus.Paid, _java));

        var result = await Service().CancelAsync(50);

        ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.Closed");
    }

    [Fact]
    public async Task CancelAsync_ConfirmedBySaleMeanwhile_ReturnsClosed()
    {
        Own(TestData.InvoiceWith(InvoiceStatus.Unpaid, _java));
        _unitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Service().CancelAsync(50);

        ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.Closed");
    }

    [Fact]
    public async Task CancelAsync_AnotherCustomersInvoice_ReturnsNotFound()
    {
        var result = await Service().CancelAsync(77);

        ResultAssert.Failed(result, ErrorType.NotFound, "Invoice.NotFound");
    }

    // ------------------------------------------------------------------ UC-67

    [Fact]
    public async Task ListAsync_WithStatusFilter_ReturnsOwnInvoices()
    {
        var unpaid = TestData.InvoiceWith(InvoiceStatus.Unpaid, _java, _sql);
        _invoices.Setup(i => i.ListByUserAsync(_customer.Id, InvoiceStatus.Unpaid, It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Page<Invoice>([unpaid], 1, 1, 10));

        var page = ResultAssert.Succeeded(await Service().ListAsync(new MyInvoiceListRequest(InvoiceStatus.Unpaid)));

        var row = Assert.Single(page.Items);
        Assert.Equal(2, row.RegistrationCount);
        Assert.Equal(550_000m, row.TotalAmount);
    }

    [Fact]
    public async Task ListAsync_NoInvoiceYet_ReturnsEmptyPage()
    {
        _invoices.Setup(i => i.ListByUserAsync(_customer.Id, null, It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Page<Invoice>([], 0, 1, 10));

        var page = ResultAssert.Succeeded(await Service().ListAsync(new MyInvoiceListRequest()));

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task ListAsync_UndefinedStatus_ReturnsValidationError()
    {
        var result = await Service().ListAsync(new MyInvoiceListRequest((InvoiceStatus)9));

        ResultAssert.Invalid(result, "Status");
    }
}
