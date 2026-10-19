using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Application.Validators.Marketing;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Sale;

/// <summary>SALE-06: UC-34 View Management Dashboard as the Sale role (DashboardService.GetAsync).</summary>
public class SaleDashboardTests
{
    private readonly Mock<IDashboardRepository> _dashboard = new();

    public SaleDashboardTests()
    {
        var today = VietnamTime.Today(DateTime.UtcNow);
        var noon = VietnamTime.StartUtc(today).AddHours(5);
        _dashboard.Setup(d => d.GetRegistrationsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(noon, RegistrationStatus.Paid), new(noon, RegistrationStatus.Submitted)]);
        _dashboard.Setup(d => d.GetPaidAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(today, 300_000m, 21)]);
        _dashboard.Setup(d => d.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private DashboardService Service(Mock<QuizPractice.Application.Abstractions.ICurrentUser> user) =>
        new(_dashboard.Object, user.Object, new DashboardRequestValidator());

    [Fact]
    public async Task GetAsync_AsSale_LinksTheRegistrationCardsAndCountsTheLastSevenDays()
    {
        var dto = ResultAssert.Succeeded(await Service(MockUser.Sale()).GetAsync(new DashboardRequest()));

        Assert.Equal("/manage/registrations", dto.Links.Registrations);
        Assert.Equal(7, dto.Trend.Count);
        Assert.Equal(1, dto.PaidRegistrations);
        Assert.Equal(1, dto.SubmittedRegistrations);
        Assert.Equal(300_000m, dto.Revenue);
    }

    [Fact]
    public async Task GetAsync_AsCustomer_ReturnsRoleNotAllowed()
    {
        var result = await Service(MockUser.Customer()).GetAsync(new DashboardRequest());

        ResultAssert.Failed(result, ErrorType.Forbidden, "Dashboard.RoleNotAllowed");
    }

    [Fact]
    public async Task GetAsync_NotSignedIn_ReturnsAuthRequired()
    {
        var result = await Service(MockUser.Guest()).GetAsync(new DashboardRequest());

        ResultAssert.Failed(result, ErrorType.Forbidden, "Auth.Required");
    }

    [Fact]
    public async Task GetAsync_RangeOf367Days_ReturnsValidationError()
    {
        var from = new DateOnly(2025, 1, 1);

        var result = await Service(MockUser.Sale()).GetAsync(new DashboardRequest(from, from.AddDays(366)));

        ResultAssert.Invalid(result, "To");
    }
}
