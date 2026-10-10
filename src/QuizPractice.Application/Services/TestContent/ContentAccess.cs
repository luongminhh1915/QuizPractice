using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>
/// BR-11: the question bank belongs to the Admin and to the Expert of each subject. One
/// rule for every Test Content service (questions, import, quizzes).
/// </summary>
internal static class ContentAccess
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

    public static readonly Error NotSignedIn =
        new(ErrorType.Forbidden, "Auth.Required", "Sign in as an Admin or an Expert to manage questions.");

    public static readonly Error NotContentRole =
        new(ErrorType.Forbidden, "Question.RoleNotAllowed", "Only the Admin and the Experts manage the question bank (BR-11).");

    public static readonly Error NotAssigned =
        new(ErrorType.Forbidden, "Question.SubjectNotAssigned", "You are not the Expert of this subject (BR-11).");
}
