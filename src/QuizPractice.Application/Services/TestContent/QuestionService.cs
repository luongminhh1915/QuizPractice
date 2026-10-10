using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.TestContent;

public class QuestionService(
    IQuestionRepository questions,
    ICurrentUser currentUser,
    IValidator<QuestionListRequest> listValidator) : IQuestionService
{
    public async Task<Result<Page<QuestionSummaryDto>>> ListAsync(QuestionListRequest request, CancellationToken ct = default)
    {
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        if (request.SubjectId is { } subjectId && await CheckSubjectAsync(subjectId, expertId, ct) is { } unreachable)
        {
            return unreachable;
        }

        var search = new QuestionSearch(expertId, request.Search, request.SubjectId, request.LessonId,
            request.DimensionIds ?? [], request.LevelId, request.Status, request.SortBy, request.Descending);
        var page = await questions.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(QuestionSummaryDto.From);
    }

    public async Task<Result<QuestionFiltersDto>> GetFiltersAsync(int? subjectId, CancellationToken ct = default)
    {
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        var subjects = await questions.GetSubjectsAsync(expertId, ct);
        var levels = await questions.GetLevelsAsync(ct);
        IReadOnlyList<QuestionLessonDto> lessons = [];
        IReadOnlyList<QuestionDimensionDto> dimensions = [];

        if (subjectId is { } id)
        {
            if (subjects.All(s => s.Id != id))
            {
                return expertId is null ? Error.NotFound("Subject", id) : NotAssigned;
            }

            lessons = (await questions.GetLessonsAsync(id, ct)).Select(QuestionLessonDto.From).ToList();
            dimensions = (await questions.GetDimensionsAsync(id, ct)).Select(QuestionDimensionDto.From).ToList();
        }

        return new QuestionFiltersDto(
            subjects.Select(s => new QuestionLookupDto(s.Id, s.Name)).ToList(),
            levels.Select(l => new QuestionLookupDto(l.Id, l.Value)).ToList(),
            lessons,
            dimensions);
    }

    /// <summary>
    /// BR-11: the question bank belongs to the Admin and to the Expert of each subject.
    /// expertId is that Expert's user id, to limit every query; null for the Admin.
    /// </summary>
    private Error? CheckRole(out int? expertId)
    {
        expertId = null;
        if (currentUser.Id is not { } userId)
        {
            return NotSignedIn;
        }

        if (currentUser.IsInRole(Roles.Admin))
        {
            return null;
        }

        if (currentUser.IsInRole(Roles.Expert))
        {
            expertId = userId;
            return null;
        }

        return NotContentRole;
    }

    /// <summary>UC-52 E1: an Expert asks for a subject not assigned to that Expert.</summary>
    private async Task<Error?> CheckSubjectAsync(int subjectId, int? expertId, CancellationToken ct)
    {
        if (expertId is null)
        {
            return null;
        }

        var subjects = await questions.GetSubjectsAsync(expertId, ct);
        return subjects.Any(s => s.Id == subjectId) ? null : NotAssigned;
    }

    private static readonly Error NotSignedIn =
        new(ErrorType.Forbidden, "Auth.Required", "Sign in as an Admin or an Expert to manage questions.");

    private static readonly Error NotContentRole =
        new(ErrorType.Forbidden, "Question.RoleNotAllowed", "Only the Admin and the Experts manage the question bank (BR-11).");

    private static readonly Error NotAssigned =
        new(ErrorType.Forbidden, "Question.SubjectNotAssigned", "You are not the Expert of this subject (BR-11).");
}
