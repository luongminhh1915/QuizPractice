using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Learning;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Learning;

/// <summary>
/// Every answer and mark is stored the moment it is given, so a reload loses nothing
/// (BR-08). The correct option never leaves the server while the attempt runs: the
/// options carry no flag, and Peek at Answer is its own call.
/// </summary>
public class QuizHandleService(
    IAttemptRepository attempts,
    IRegistrationRepository registrations,
    IPrivateFileStorage mediaFiles,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IQuizHandleService
{
    public async Task<Result<QuizHandleDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var (attempt, refusal) = await LoadRunningAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        var label = (await AttemptLabels.LabelsAsync([attempt!], attempts, ct))[attempt!.Id];
        return new QuizHandleDto(
            attempt!.Id,
            attempt.Subject.Name,
            label.Title,
            attempt.Mode,
            Utc(attempt.StartedAt),
            AttemptScoring.EndsAt(attempt),
            DateTime.UtcNow,
            attempt.CurrentOrder,
            attempt.Questions.Select(ToDto).ToList());
    }

    public async Task<Result<HandleProgressDto>> AnswerAsync(int id, int order, AnswerRequest request, CancellationToken ct = default)
    {
        var (attempt, refusal) = await LoadRunningAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        if (QuestionAt(attempt!, order) is not { } row)
        {
            return Error.NotFound("Question", order);
        }

        if (row.Question.Options.All(o => o.Id != request.OptionId))
        {
            return Error.Invalid(nameof(request.OptionId), "Choose one of the options of this question.");
        }

        // A1: another choice replaces the one recorded; the question becomes the current one.
        row.ChosenOptionId = request.OptionId;
        attempt!.CurrentOrder = row.DisplayOrder;
        await unitOfWork.SaveChangesAsync(ct);
        return new HandleProgressDto(row.DisplayOrder, row.ChosenOptionId, row.IsMarked);
    }

    public async Task<Result<HandleProgressDto>> MoveAsync(int id, MoveRequest request, CancellationToken ct = default)
    {
        var (attempt, refusal) = await LoadRunningAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        // Previous, Next (A1, A2) or a number picked in Review Progress (A3).
        if (QuestionAt(attempt!, request.Order) is not { } row)
        {
            return Error.Invalid(nameof(request.Order), $"Choose a question from 1 to {attempt!.Questions.Count}.");
        }

        if (attempt!.CurrentOrder != row.DisplayOrder)
        {
            attempt.CurrentOrder = row.DisplayOrder;
            await unitOfWork.SaveChangesAsync(ct);
        }

        return new HandleProgressDto(row.DisplayOrder, row.ChosenOptionId, row.IsMarked);
    }

    public async Task<Result<HandleProgressDto>> MarkAsync(int id, int order, MarkRequest request, CancellationToken ct = default)
    {
        var (attempt, refusal) = await LoadRunningAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        if (QuestionAt(attempt!, order) is not { } row)
        {
            return Error.NotFound("Question", order);
        }

        // A2: only the marked state changes; the recorded answer stays.
        row.IsMarked = request.Marked;
        attempt!.CurrentOrder = row.DisplayOrder;
        await unitOfWork.SaveChangesAsync(ct);
        return new HandleProgressDto(row.DisplayOrder, row.ChosenOptionId, row.IsMarked);
    }

    public async Task<Result<PeekDto>> PeekAsync(int id, int order, CancellationToken ct = default)
    {
        var (attempt, refusal) = await LoadRunningAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        // BR-07, A1: the exam screen has no Peek button; the server refuses it as well.
        if (attempt!.Mode == AttemptMode.Exam)
        {
            return new Error(ErrorType.Forbidden, "Attempt.NoPeek", "Peek at Answer is not available during a simulation exam.");
        }

        if (QuestionAt(attempt, order) is not { } row)
        {
            return Error.NotFound("Question", order);
        }

        // The recorded answers are unchanged: peeking stores nothing.
        var options = row.Question.Options;
        var correct = options.FindIndex(o => o.IsCorrect);
        return new PeekDto(
            options[correct].Id,
            Letter(correct),
            options[correct].Content,
            row.Question.Explanation,
            row.Question.Dimensions.Select(d => $"{d.Dimension.Type.Value}: {d.Dimension.Name}").Order().ToList());
    }

    public async Task<Result<AttemptResultDto>> SubmitAsync(int id, CancellationToken ct = default)
    {
        var (attempt, refusal) = await LoadRunningAsync(id, ct, submitting: true);
        if (refusal is not null)
        {
            return refusal;
        }

        // Steps 5 and 6. The three confirmations of BR-09 are the page's; every case ends
        // here. E1: a countdown that reached zero submits too, finished at the end time.
        var now = DateTime.UtcNow;
        var end = AttemptScoring.EndsAt(attempt!);
        AttemptScoring.Score(attempt!, end is { } e && now > e ? e : now);
        await unitOfWork.SaveChangesAsync(ct);
        return ResultOf(attempt!);
    }

    public async Task<Result<MediaFile>> GetMediaAsync(int id, int order, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        // Also after the attempt is scored: Quiz Review shows the same media.
        var attempt = await attempts.GetOwnAsync(id, userId, ct);
        if (attempt is null)
        {
            return CustomerAccess.NotFound("Attempt", id);
        }

        var question = QuestionAt(attempt, order)?.Question;
        var type = QuestionMedia.TypeOf(question?.Media);
        var content = type is null ? null : mediaFiles.OpenRead(question!.Media!);
        return content is null
            ? new Error(ErrorType.NotFound, "Question.NoMedia", $"Question {order} of this attempt has no media file.")
            : new MediaFile(content, type!.ContentType);
    }

    /// <summary>
    /// The customer's own attempt, still running, on a subject the customer may still
    /// reach; or the error that stops UC-24 to UC-29. A simulation exam past its end time
    /// is scored here as it stands (UC-30 E1), unless the call is the submission itself.
    /// </summary>
    private async Task<(Attempt? Attempt, Error? Refusal)> LoadRunningAsync(int id, CancellationToken ct, bool submitting = false)
    {
        if (currentUser.Id is not { } userId)
        {
            return (null, CustomerAccess.SignInRequired);
        }

        var attempt = await attempts.GetOwnAsync(id, userId, ct);
        if (attempt is null)
        {
            return (null, CustomerAccess.NotFound("Attempt", id));
        }

        if (attempt.Status != AttemptStatus.InProgress)
        {
            return (null, Error.Conflict("Attempt.Closed", "This attempt has been scored. Its result is in Practice Details."));
        }

        if (!submitting && AttemptScoring.CloseIfTimeUp(attempt, DateTime.UtcNow))
        {
            await unitOfWork.SaveChangesAsync(ct);
            return (null, Error.Conflict("Attempt.TimeUp", "Time is up: the exam was submitted with the answers recorded so far."));
        }

        // E1: the access ended while the attempt was running; what is stored stays.
        var reachable = await registrations.ListAccessibleSubjectsAsync(userId, VietnamTime.Today(DateTime.UtcNow), ct);
        if (reachable.All(s => s.Id != attempt.SubjectId))
        {
            return (null, new Error(ErrorType.Forbidden, "Attempt.AccessEnded",
                $"Your access to {attempt.Subject.Name} has ended, so this attempt cannot continue. Your answers so far are kept."));
        }

        return (attempt, null);
    }

    internal static AttemptResultDto ResultOf(Attempt attempt) =>
        new(attempt.Id, AttemptScoring.CorrectCount(attempt), attempt.Questions.Count, attempt.Score ?? 0, attempt.PassRate ?? 0,
            AttemptScoring.Passed(attempt));

    private static AttemptQuestion? QuestionAt(Attempt attempt, int order) =>
        attempt.Questions.FirstOrDefault(q => q.DisplayOrder == order);

    private static HandleQuestionDto ToDto(AttemptQuestion row) =>
        new(row.DisplayOrder,
            row.QuestionId,
            row.Question.Content,
            QuestionMedia.TypeOf(row.Question.Media)?.Kind,
            row.Question.Options.Select((o, i) => new HandleOptionDto(o.Id, Letter(i), o.Content)).ToList(),
            row.ChosenOptionId,
            row.IsMarked);

    /// <summary>A, B, C… by display order (SRS III 4.1, Answer Options).</summary>
    internal static string Letter(int index) => ((char)('A' + index)).ToString();

    internal static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
