using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Sale;

/// <summary>SALE-05: UC-69 Confirm Invoice Payment (InvoiceService.ConfirmAsync, SaveNotesAsync, GetAsync).</summary>
public class ConfirmInvoiceTests : InvoiceServiceFixture
{
    private static DateOnly Today => VietnamTime.Today(DateTime.UtcNow);

    private readonly Registration _java = TestData.Registration(1, package: TestData.Package(100, months: 6));
    private readonly Registration _sql = TestData.Registration(2, package: TestData.Package(200, months: 12, subjectId: 2));

    private Invoice UnpaidInvoice() => Stored(TestData.InvoiceWith(InvoiceStatus.Unpaid, _java, _sql));

    [Fact]
    public async Task ConfirmAsync_UnpaidInvoice_MarksInvoiceAndEveryRegistrationPaid()
    {
        var invoice = UnpaidInvoice();

        var dto = ResultAssert.Succeeded(await Service().ConfirmAsync(50, new InvoiceNotesRequest("Found on statement 12/10")));

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAt);
        Assert.Equal(TestData.SaleId, invoice.ConfirmedById);
        Assert.Equal("Found on statement 12/10", invoice.Notes);
        Assert.All(dto.Registrations, r => Assert.Equal(RegistrationStatus.Paid, r.Status));
        UnitOfWork.Verify(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_UnpaidInvoice_GivesEachRegistrationItsOwnValidity()
    {
        UnpaidInvoice();

        await Service().ConfirmAsync(50, new InvoiceNotesRequest(null));

        Assert.Equal(Today, _java.ValidFrom);
        Assert.Equal(Today.AddMonths(6), _java.ValidTo);
        Assert.Equal(Today.AddMonths(12), _sql.ValidTo);
        Assert.Equal(TestData.SaleId, _sql.UpdatedById);
    }

    [Fact]
    public async Task ConfirmAsync_CancelledByTheCustomer_ReturnsInvoiceCancelled()
    {
        var invoice = Stored(TestData.InvoiceWith(InvoiceStatus.Cancelled, _java));

        var result = await Service().ConfirmAsync(50, new InvoiceNotesRequest(null));

        ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.Cancelled");
        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);
        UnitOfWork.Verify(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_AlreadyPaid_ReturnsInvoicePaidNamingTheConfirmer()
    {
        var invoice = TestData.InvoiceWith(InvoiceStatus.Paid, _java);
        invoice.ConfirmedBy = TestData.SaleUser(TestData.OtherSaleId, "sale2@gmail.com");
        Stored(invoice);

        var result = await Service().ConfirmAsync(50, new InvoiceNotesRequest(null));

        var error = ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.Paid");
        Assert.Contains("sale2@gmail.com", error.Message);
    }

    [Fact]
    public async Task ConfirmAsync_RegistrationWithoutDuration_ReturnsNoDurationAndChangesNothing()
    {
        var noDuration = TestData.Registration(3, package: TestData.Package(300, months: 0, subjectId: 3));
        var invoice = Stored(TestData.InvoiceWith(InvoiceStatus.Unpaid, _java, noDuration));

        var result = await Service().ConfirmAsync(50, new InvoiceNotesRequest(null));

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.NoDuration");
        Assert.Equal(InvoiceStatus.Unpaid, invoice.Status);
        Assert.Equal(RegistrationStatus.Submitted, _java.Status);
        Assert.Null(_java.ValidFrom);
    }

    [Fact]
    public async Task ConfirmAsync_ChangedMeanwhile_ReturnsInvoiceChanged()
    {
        UnpaidInvoice();
        UnitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Service().ConfirmAsync(50, new InvoiceNotesRequest(null));

        ResultAssert.Failed(result, ErrorType.Conflict, "Invoice.Changed");
    }

    [Fact]
    public async Task ConfirmAsync_BlankNotes_KeepTheEarlierNotes()
    {
        var invoice = UnpaidInvoice();
        invoice.Notes = "Customer called on 11/10";

        await Service().ConfirmAsync(50, new InvoiceNotesRequest("  "));

        Assert.Equal("Customer called on 11/10", invoice.Notes);
    }

    [Fact]
    public async Task ConfirmAsync_NotesOf501Characters_ReturnsValidationError()
    {
        UnpaidInvoice();

        var result = await Service().ConfirmAsync(50, new InvoiceNotesRequest(new string('n', 501)));

        ResultAssert.Invalid(result, "Notes");
    }

    [Fact]
    public async Task ConfirmAsync_UnknownInvoice_ReturnsNotFound()
    {
        var result = await Service().ConfirmAsync(404, new InvoiceNotesRequest(null));

        ResultAssert.Failed(result, ErrorType.NotFound, "Invoice.NotFound");
    }

    [Fact]
    public async Task ConfirmAsync_NotSignedIn_ReturnsUnauthorized()
    {
        UnpaidInvoice();

        var result = await Service(MockUser.Guest()).ConfirmAsync(50, new InvoiceNotesRequest(null));

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
    }
}
