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
    public async Task ConfirmAsync_BlankNotes_KeepTheEarlierNotes()
    {
        var invoice = UnpaidInvoice();
        invoice.Notes = "Customer called on 11/10";

        await Service().ConfirmAsync(50, new InvoiceNotesRequest("  "));

        Assert.Equal("Customer called on 11/10", invoice.Notes);
    }
}
