using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Marketing;

/// <summary>
/// UC-34 (SRS III 5.1). Every figure is counted in Vietnam days. Registrations and the
/// trend use the day an order was made; revenue, revenue by category and new buyers use
/// the day a registration became Paid (valid_from, BR-14).
/// </summary>
public class DashboardService(
    IDashboardRepository dashboard,
    ICurrentUser currentUser,
    IValidator<DashboardRequest> validator) : IDashboardService
{
    /// <summary>Step 2 and A2: the last seven days, today included.</summary>
    public const int DefaultDays = 7;

    private static readonly string[] AllowedRoles = [Roles.Marketing, Roles.Sale, Roles.Admin];

    public async Task<Result<DashboardDto>> GetAsync(DashboardRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is null)
        {
            return new Error(ErrorType.Forbidden, "Auth.Required", "Sign in as Marketing, Sale or Admin to see the dashboard.");
        }

        if (!AllowedRoles.Any(currentUser.IsInRole))
        {
            return new Error(ErrorType.Forbidden, "Dashboard.RoleNotAllowed", "The dashboard is for the Marketing, Sale and Admin roles.");
        }

        // E1: the API refuses the range; the page keeps the previous one.
        if (await validator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var (from, to) = request.From is { } f && request.To is { } t
            ? (f, t)
            : (VietnamTime.Today(DateTime.UtcNow).AddDays(1 - DefaultDays), VietnamTime.Today(DateTime.UtcNow));
        var fromUtc = VietnamTime.StartUtc(from);
        var toUtc = VietnamTime.StartUtc(to.AddDays(1));

        var (newSubjects, totalSubjects) = await dashboard.CountSubjectsAsync(fromUtc, toUtc, ct);
        var registrations = await dashboard.GetRegistrationsAsync(fromUtc, toUtc, ct);
        var paid = await dashboard.GetPaidAsync(from, to, ct);
        var newCustomers = await dashboard.CountNewCustomersAsync(fromUtc, toUtc, ct);
        var newBuyers = await dashboard.CountFirstTimeBuyersAsync(from, to, ct);
        var categories = await dashboard.GetCategoriesAsync(ct);

        // One point per day, also the days without orders (A1: the trend may be all zero).
        var byDay = registrations.ToLookup(r => VietnamTime.DayOf(r.RegisteredAt));
        var trend = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(i => from.AddDays(i))
            .Select(day => new TrendPointDto(day, byDay[day].Count(r => r.Status == RegistrationStatus.Paid), byDay[day].Count()))
            .ToList();

        var revenueByCategory = categories
            .Select(c => new CategoryRevenueDto(c.Value, paid.Where(p => p.CategoryId == c.Id).Sum(p => p.TotalCost)))
            .ToList();

        return new DashboardDto(
            from, to, newSubjects, totalSubjects,
            registrations.Count(r => r.Status == RegistrationStatus.Paid),
            registrations.Count(r => r.Status == RegistrationStatus.Submitted),
            registrations.Count(r => r.Status == RegistrationStatus.Cancelled),
            paid.Sum(p => p.TotalCost), newCustomers, newBuyers, trend, revenueByCategory, LinksFor());
    }

    /// <summary>
    /// SRS III 5.1 and OI-10: a card is a link only for a role that may open the screen behind
    /// it. Sale reaches the registrations, the Admin the subjects and the users; Marketing none.
    /// TODO(sale, course-content, admin): these screens come with UC-59, UC-41 and UC-62.
    /// </summary>
    private DashboardLinks LinksFor() => currentUser.Role switch
    {
        Roles.Sale => new DashboardLinks(null, "/manage/registrations", "/manage/registrations?status=Paid", null),
        Roles.Admin => new DashboardLinks("/manage/subjects", null, null, "/manage/users"),
        _ => new DashboardLinks(null, null, null, null),
    };
}
