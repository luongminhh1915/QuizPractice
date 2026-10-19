using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Sale;

/// <summary>SALE-01: UC-59 View Registrations List (RegistrationService.ListAsync, GetFiltersAsync).</summary>
public class RegistrationListTests : RegistrationServiceFixture
{
    private RegistrationSearch? _search;
    private PageRequest? _page;

    public RegistrationListTests()
    {
        var rows = new List<Registration> { TestData.Registration(1), TestData.Registration(2, RegistrationStatus.Paid) };
        Registrations
            .Setup(r => r.SearchAsync(It.IsAny<RegistrationSearch>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrationSearch, PageRequest, CancellationToken>((s, p, _) => (_search, _page) = (s, p))
            .ReturnsAsync(new Page<Registration>(rows, 2, 1, 10));
    }

    [Fact]
    public async Task ListAsync_WithoutFilter_ReturnsEveryRegistration()
    {
        var page = ResultAssert.Succeeded(await Service().ListAsync(new RegistrationListRequest()));

        Assert.Equal(2, page.TotalCount);
        Assert.Equal([1, 2], page.Items.Select(r => r.Id));
        Assert.Equal(new RegistrationSearch(null, null, null, null, null, null, false), _search);
        Assert.Equal(new PageRequest(1, 10), _page);
    }

    [Fact]
    public async Task ListAsync_WithSearchKeyword_PassesTheKeyword()
    {
        await Service().ListAsync(new RegistrationListRequest(Search: "an.tran"));

        Assert.Equal("an.tran", _search!.Text);
    }

    [Fact]
    public async Task ListAsync_WithStatusFilter_PassesTheStatus()
    {
        await Service().ListAsync(new RegistrationListRequest(Status: RegistrationStatus.Paid));

        Assert.Equal(RegistrationStatus.Paid, _search!.Status);
    }

    [Fact]
    public async Task ListAsync_WithSubjectFilterAndSort_PassesThem()
    {
        await Service().ListAsync(new RegistrationListRequest(SubjectId: 1, SortBy: "TotalCost", Descending: true));

        Assert.Equal(1, _search!.SubjectId);
        Assert.Equal("TotalCost", _search.SortBy);
        Assert.True(_search.Descending);
    }

    [Fact]
    public async Task ListAsync_DateRange_CoversWholeVietnamDays()
    {
        await Service().ListAsync(new RegistrationListRequest(From: new DateOnly(2026, 10, 1), To: new DateOnly(2026, 10, 5)));

        // 00:00 in Vietnam (UTC+7) is 17:00 UTC the day before; To runs to the end of its day.
        Assert.Equal(new DateTime(2026, 9, 30, 17, 0, 0), _search!.From);
        Assert.Equal(new DateTime(2026, 10, 5, 17, 0, 0), _search.Before);
    }

    [Fact]
    public async Task ListAsync_FromEqualsTo_IsAccepted()
    {
        var day = new DateOnly(2026, 10, 1);

        var result = await Service().ListAsync(new RegistrationListRequest(From: day, To: day));

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.FromDays(1), _search!.Before - _search.From);
    }

    [Fact]
    public async Task ListAsync_ToBeforeFrom_ReturnsValidationError()
    {
        var result = await Service().ListAsync(new RegistrationListRequest(From: new DateOnly(2026, 10, 5), To: new DateOnly(2026, 10, 1)));

        ResultAssert.Invalid(result, "To");
        Assert.Null(_search);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ListAsync_PageSizeOutsideOneToHundred_ReturnsValidationError(int pageSize)
    {
        var result = await Service().ListAsync(new RegistrationListRequest(PageSize: pageSize));

        ResultAssert.Invalid(result, "PageSize");
    }

    [Fact]
    public async Task ListAsync_PageSizeHundred_IsAccepted()
    {
        var result = await Service().ListAsync(new RegistrationListRequest(PageSize: 100));

        Assert.True(result.IsSuccess);
        Assert.Equal(100, _page!.PageSize);
    }

    [Fact]
    public async Task ListAsync_UnknownSortColumn_ReturnsValidationError()
    {
        var result = await Service().ListAsync(new RegistrationListRequest(SortBy: "password"));

        ResultAssert.Invalid(result, "SortBy");
    }

    [Fact]
    public async Task GetFiltersAsync_ReturnsEverySubject()
    {
        Quizzes.Setup(q => q.GetSubjectsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.Subject(1, "Java OOP"), TestData.Subject(2, "SQL Fundamentals")]);

        var filters = ResultAssert.Succeeded(await Service().GetFiltersAsync());

        Assert.Equal(["Java OOP", "SQL Fundamentals"], filters.Subjects.Select(s => s.Name));
    }
}
