using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Sale;

/// <summary>SALE-04: UC-68 View Invoices List (InvoiceService.ListAsync).</summary>
public class InvoiceListTests : InvoiceServiceFixture
{
    private InvoiceSearch? _search;

    public InvoiceListTests()
    {
        var unpaid = TestData.InvoiceWith(InvoiceStatus.Unpaid, TestData.Registration(1), TestData.Registration(2));
        Invoices
            .Setup(i => i.SearchAsync(It.IsAny<InvoiceSearch>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceSearch, PageRequest, CancellationToken>((s, _, _) => _search = s)
            .ReturnsAsync(new Page<Invoice>([unpaid], 1, 1, 10));
    }

    [Fact]
    public async Task ListAsync_WithoutFilter_ReturnsInvoicesWithTheirRegistrationCount()
    {
        var page = ResultAssert.Succeeded(await Service().ListAsync(new InvoiceListRequest()));

        var row = Assert.Single(page.Items);
        Assert.Equal("QP7K2M9X", row.Code);
        Assert.Equal("customer@gmail.com", row.CustomerEmail);
        Assert.Equal(2, row.RegistrationCount);
        Assert.Equal(600_000m, row.TotalAmount);
        Assert.Equal(new InvoiceSearch(null, null, null, null), _search);
    }

    [Fact]
    public async Task ListAsync_WithStatusFilter_PassesTheStatus()
    {
        await Service().ListAsync(new InvoiceListRequest(Status: InvoiceStatus.Unpaid));

        Assert.Equal(InvoiceStatus.Unpaid, _search!.Status);
    }

    [Fact]
    public async Task ListAsync_SearchByCode_PassesTheText()
    {
        await Service().ListAsync(new InvoiceListRequest(Search: "QP7K"));

        Assert.Equal("QP7K", _search!.Text);
    }

    [Fact]
    public async Task ListAsync_DateRange_CoversWholeVietnamDays()
    {
        await Service().ListAsync(new InvoiceListRequest(From: new DateOnly(2026, 10, 1), To: new DateOnly(2026, 10, 1)));

        Assert.Equal(new DateTime(2026, 9, 30, 17, 0, 0), _search!.From);
        Assert.Equal(new DateTime(2026, 10, 1, 17, 0, 0), _search.Before);
    }

    [Fact]
    public async Task ListAsync_ToBeforeFrom_ReturnsValidationError()
    {
        var result = await Service().ListAsync(new InvoiceListRequest(From: new DateOnly(2026, 10, 2), To: new DateOnly(2026, 10, 1)));

        ResultAssert.Invalid(result, "To");
        Assert.Null(_search);
    }

    [Fact]
    public async Task ListAsync_SearchLongerThan200Characters_ReturnsValidationError()
    {
        var result = await Service().ListAsync(new InvoiceListRequest(Search: new string('x', 201)));

        ResultAssert.Invalid(result, "Search");
    }
}
