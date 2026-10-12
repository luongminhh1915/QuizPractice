using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Customer;

/// <summary>
/// An exam is offered while it is Active, of type Simulation, and its subject is one the
/// customer may reach today (BR-05, BR-23). A lesson quiz is not listed: no customer
/// screen opens one yet.
/// </summary>
public class SimulationExamService(
    IRegistrationRepository registrations,
    IQuizRepository quizzes,
    ICurrentUser currentUser,
    IValidator<ExamListRequest> listValidator) : ISimulationExamService
{
    public async Task<Result<Page<ExamSummaryDto>>> ListAsync(ExamListRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // E1 is an empty page with the criteria kept; E2 shows through the empty filters.
        var reachable = (await AccessibleSubjectsAsync(userId, ct)).Select(s => s.Id).ToList();
        var search = new ExamSearch(reachable, request.Search, request.SubjectId, request.SortBy, request.Descending);
        var page = await quizzes.SearchExamsAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(q => new ExamSummaryDto(q.Id, q.Subject.Name, q.Name, q.Level.Value, q.QuestionCount, q.DurationMin, q.PassRate));
    }

    public async Task<Result<ExamFiltersDto>> GetFiltersAsync(CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        var subjects = await AccessibleSubjectsAsync(userId, ct);
        return new ExamFiltersDto(subjects.Select(s => new PracticeSubjectDto(s.Id, s.Name)).ToList());
    }

    private Task<IReadOnlyList<Subject>> AccessibleSubjectsAsync(int userId, CancellationToken ct) =>
        registrations.ListAccessibleSubjectsAsync(userId, VietnamTime.Today(DateTime.UtcNow), ct);
}
