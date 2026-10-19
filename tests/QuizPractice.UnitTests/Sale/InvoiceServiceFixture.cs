using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Services.Sale;
using QuizPractice.Application.Validators.Sale;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Sale;

/// <summary>The mocks every InvoiceService test starts from (UC-68, UC-69).</summary>
public abstract class InvoiceServiceFixture
{
    protected readonly Mock<IInvoiceRepository> Invoices = new();
    protected readonly Mock<IUnitOfWork> UnitOfWork = new();

    protected InvoiceServiceFixture() =>
        UnitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

    protected InvoiceService Service(Mock<ICurrentUser>? user = null) =>
        new(Invoices.Object, UnitOfWork.Object, (user ?? MockUser.Sale()).Object,
            new InvoiceListRequestValidator(), new InvoiceNotesRequestValidator());

    protected Invoice Stored(Invoice invoice)
    {
        Invoices.Setup(i => i.GetForSaleAsync(invoice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
        return invoice;
    }
}
