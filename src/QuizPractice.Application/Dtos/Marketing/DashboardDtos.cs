namespace QuizPractice.Application.Dtos.Marketing;

/// <summary>
/// The range of the Management Dashboard (UC-34), in Vietnam days, both ends included.
/// Leaving either out gives the default: the last seven days (step 2, A2).
/// </summary>
public sealed record DashboardRequest(DateOnly? From = null, DateOnly? To = null);

/// <summary>One day of the order trend: orders made that day, and how many of them are Paid now.</summary>
public sealed record TrendPointDto(DateOnly Date, int Paid, int All);

/// <summary>The revenue of one Subject Category (BR-16).</summary>
public sealed record CategoryRevenueDto(string Category, decimal Revenue);

/// <summary>
/// Where a card leads, for the signed-in role only (SRS III 5.1, OI-10): Sale reaches
/// the registrations, the Admin the subjects and the users; Marketing sees no link.
/// </summary>
public sealed record DashboardLinks(string? Subjects, string? Registrations, string? Revenue, string? Customers);

/// <summary>
/// The figures of UC-34 over the range. Registrations are those made in the range, by
/// current status; revenue and buyers count the registrations that became Paid in it.
/// </summary>
public sealed record DashboardDto(
    DateOnly From,
    DateOnly To,
    int NewSubjects,
    int TotalSubjects,
    int PaidRegistrations,
    int SubmittedRegistrations,
    int CancelledRegistrations,
    decimal Revenue,
    int NewCustomers,
    int NewBuyers,
    IReadOnlyList<TrendPointDto> Trend,
    IReadOnlyList<CategoryRevenueDto> RevenueByCategory,
    DashboardLinks Links);
