using QuizPractice.Application.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Services.Sale;

/// <summary>
/// BR-14: a registration that becomes Paid opens its subject from today (Vietnam) for the
/// months of its package. Shared by UC-61 (one registration) and UC-69 (every
/// registration of an invoice), so both give the same validity.
/// </summary>
internal static class RegistrationSettlement
{
    /// <summary>UC-61 E1, UC-69 E2: a package without an access duration cannot open access.</summary>
    public static Error? CheckPayable(Registration r) =>
        r.Package is { AccessMonths: > 0 }
            ? null
            : Error.Conflict("Registration.NoDuration",
                $"Registration #{r.Id} has no package with an access duration, so it cannot become Paid.");

    public static void MarkPaid(Registration r, int byUserId, DateTime utcNow)
    {
        var today = VietnamTime.Today(utcNow);
        r.Status = RegistrationStatus.Paid;
        r.ValidFrom = today;
        r.ValidTo = today.AddMonths(r.Package.AccessMonths);
        r.UpdatedById = byUserId;
    }
}
