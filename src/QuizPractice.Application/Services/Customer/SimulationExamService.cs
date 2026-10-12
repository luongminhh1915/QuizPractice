using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Learning;
using QuizPractice.Application.Services.TestContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
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
    IAttemptRepository attempts,
    IUnitOfWork unitOfWork,
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

    public async Task<Result<ExamDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var (quiz, userId, refusal) = await LoadOfferedAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        var open = await OpenAttemptAsync(userId, id, ct);
        return new ExamDetailsDto(quiz!.Id, quiz.Subject.Name, quiz.Name, quiz.Level.Value, quiz.QuestionCount, quiz.DurationMin,
            quiz.PassRate, quiz.Description, open?.Id);
    }

    public async Task<Result<AttemptCreatedDto>> StartAsync(int id, CancellationToken ct = default)
    {
        // E1: the access may have ended since the list was loaded.
        var (quiz, userId, refusal) = await LoadOfferedAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        // A2 (Duck 12/10): an unfinished attempt is resumed, never left for a new set.
        if (await OpenAttemptAsync(userId, id, ct) is not null)
        {
            return Error.Conflict("Exam.Unfinished", "You have an unfinished attempt at this exam. Resume it instead of starting a new one.");
        }

        // Step 4 (OI-07): a new set on every start, so a second sitting meets other questions.
        // E2: a group no longer holds enough shown questions since the exam was saved.
        var groups = await QuestionDraw.LoadGroupsAsync(quizzes, quiz!.SubjectId, quiz.SelectionType, ct);
        var drawn = groups is null
            ? null
            : QuestionDraw.Draw(quiz.Rules.Select(r => (r.GroupId, (int)r.QuestionCount)).ToList(), groups).Drawn;
        if (drawn is null || drawn.Count == 0)
        {
            return Error.Conflict("Exam.NotAvailable", $"\"{quiz.Name}\" is not available for now. Try another exam or come back later.");
        }

        var attempt = new Attempt
        {
            UserId = userId,
            SubjectId = quiz.SubjectId,
            QuizId = quiz.Id,
            Mode = AttemptMode.Exam,
            QuestionCount = (short)drawn.Count,
            StartedAt = DateTime.UtcNow,
            Status = AttemptStatus.InProgress,
        };
        for (var i = 0; i < drawn.Count; i++)
        {
            attempt.Questions.Add(new AttemptQuestion { Attempt = attempt, QuestionId = drawn[i], DisplayOrder = (short)(i + 1) });
        }

        attempts.Add(attempt);
        await unitOfWork.SaveChangesAsync(ct);
        return new AttemptCreatedDto(attempt.Id);
    }

    /// <summary>An Active simulation exam of a subject the customer may reach; or the error that stops UC-23.</summary>
    private async Task<(Quiz? Quiz, int UserId, Error? Refusal)> LoadOfferedAsync(int id, CancellationToken ct)
    {
        if (currentUser.Id is not { } userId)
        {
            return (null, 0, CustomerAccess.SignInRequired);
        }

        var quiz = await quizzes.GetWithRulesAsync(id, ct);
        if (quiz is null || quiz.Status != ActiveStatus.Active || quiz.Type.Value != SettingValues.TestTypes.Simulation)
        {
            return (null, userId, Error.NotFound("Exam", id));
        }

        if ((await AccessibleSubjectsAsync(userId, ct)).All(s => s.Id != quiz.SubjectId))
        {
            return (null, userId, CustomerAccess.NoAccess);
        }

        return (quiz, userId, null);
    }

    /// <summary>The customer's attempt at this exam still running; one past its end time is scored first (UC-30 E1).</summary>
    private async Task<Attempt?> OpenAttemptAsync(int userId, int quizId, CancellationToken ct)
    {
        var open = (await attempts.ListOpenExamsAsync(userId, ct)).Where(a => a.QuizId == quizId).ToList();
        var now = DateTime.UtcNow;
        if (open.Count(a => AttemptScoring.CloseIfTimeUp(a, now)) > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        return open.Where(a => a.Status == AttemptStatus.InProgress).OrderByDescending(a => a.StartedAt).FirstOrDefault();
    }

    private Task<IReadOnlyList<Subject>> AccessibleSubjectsAsync(int userId, CancellationToken ct) =>
        registrations.ListAccessibleSubjectsAsync(userId, VietnamTime.Today(DateTime.UtcNow), ct);
}
