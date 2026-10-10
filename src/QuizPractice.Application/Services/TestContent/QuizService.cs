using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.TestContent;

public class QuizService(
    IQuizRepository quizzes,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<QuizListRequest> listValidator) : IQuizService
{
    public async Task<Result<Page<QuizSummaryDto>>> ListAsync(QuizListRequest request, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // UC-56 E1: an Expert asks for a subject not assigned to that Expert.
        if (request.SubjectId is { } subjectId && expertId is not null
            && (await quizzes.GetSubjectsAsync(expertId, ct)).All(s => s.Id != subjectId))
        {
            return ContentAccess.NotAssigned;
        }

        var search = new QuizSearch(expertId, request.Search, request.SubjectId, request.TypeId, request.SortBy, request.Descending);
        var page = await quizzes.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        var taken = await quizzes.GetTakenAsync(page.Items.Select(q => q.Id).ToList(), ct);
        return page.Map(q => QuizSummaryDto.From(q, taken.Contains(q.Id)));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var quiz = await quizzes.GetByIdAsync(id, ct);
        if (quiz is null)
        {
            return Error.NotFound("Quiz", id);
        }

        // E1: an Expert on a quiz of a subject not assigned to that Expert.
        if (expertId is not null && quiz.Subject.ExpertId != expertId)
        {
            return ContentAccess.NotAssigned;
        }

        // E2 (BR-13): the stored results keep their quiz.
        if ((await quizzes.GetTakenAsync([id], ct)).Count > 0)
        {
            return Error.Conflict("Quiz.Taken",
                $"Customers have taken \"{quiz.Name}\", so it is kept with their results (BR-13). Set it Inactive instead.");
        }

        // E3: a lesson of type Quiz would point at nothing.
        var lessons = await quizzes.GetLessonsUsingAsync(id, ct);
        if (lessons.Count > 0)
        {
            var names = string.Join(", ", lessons.Select(l => $"\"{l}\""));
            return Error.Conflict("Quiz.UsedByLesson",
                $"\"{quiz.Name}\" is opened by lesson {names}. Point the lesson to another quiz or remove it in Subject Details first.");
        }

        quizzes.Remove(quiz);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<QuizFiltersDto>> GetFiltersAsync(CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var subjects = await quizzes.GetSubjectsAsync(expertId, ct);
        var types = await quizzes.GetTypesAsync(ct);
        return new QuizFiltersDto(
            subjects.Select(s => new QuestionLookupDto(s.Id, s.Name)).ToList(),
            types.Select(t => new QuestionLookupDto(t.Id, t.Value)).ToList());
    }
}
