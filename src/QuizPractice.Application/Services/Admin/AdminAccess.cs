using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Services.Admin;

/// <summary>Users, Settings and the receiving bank account are the Admin's (F#37 to F#40, UC-70).</summary>
internal static class AdminAccess
{
    public static Error? Check(ICurrentUser user) =>
        user.Id is null || !user.IsInRole(Roles.Admin)
            ? new Error(ErrorType.Forbidden, "Admin.Required", "Only the Admin manages users and settings.")
            : null;
}
