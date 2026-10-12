using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;

namespace QuizPractice.Application.Services.CourseContent;

/// <summary>
/// BR-11: a subject and its content belong to the Admin and to the Expert the subject is
/// assigned to. One rule for every Course Content service (subjects, dimensions, packages, lessons).
/// </summary>
internal static class CourseAccess
{
    /// <summary>expertId is that Expert's user id, to limit every query; null for the Admin.</summary>
    public static Error? Check(ICurrentUser user, out int? expertId)
    {
        expertId = null;
        if (user.Id is not { } userId)
        {
            return NotSignedIn;
        }

        if (user.IsInRole(Roles.Admin))
        {
            return null;
        }

        if (user.IsInRole(Roles.Expert))
        {
            expertId = userId;
            return null;
        }

        return NotContentRole;
    }

    /// <summary>The Admin only: creating a subject, its owner, its status and its packages (BR-12).</summary>
    public static Error? CheckAdmin(ICurrentUser user)
    {
        if (Check(user, out var expertId) is { } denied)
        {
            return denied;
        }

        return expertId is null ? null : AdminOnly;
    }

    /// <summary>E1 of UC-44, UC-46, UC-48 to UC-51: an Expert on a subject not assigned to that Expert.</summary>
    public static Error? CheckSubject(Subject subject, int? expertId) =>
        expertId is not null && subject.ExpertId != expertId ? NotAssigned : null;

    public static readonly Error NotSignedIn =
        new(ErrorType.Forbidden, "Auth.Required", "Sign in as an Admin or an Expert to manage subjects.");

    public static readonly Error NotContentRole =
        new(ErrorType.Forbidden, "Subject.RoleNotAllowed", "Only the Admin and the Experts manage subjects (BR-11).");

    public static readonly Error NotAssigned =
        new(ErrorType.Forbidden, "Subject.NotAssigned", "You are not the Expert of this subject (BR-11).");

    public static readonly Error AdminOnly =
        new(ErrorType.Forbidden, "Subject.AdminOnly", "Only the Admin may do this (BR-12).");
}
