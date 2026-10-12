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

        var titles = await AttemptLabels.TitlesAsync([attempt!], attempts, ct);
        return new QuizHandleDto(
            attempt!.Id,
            attempt.Subject.Name,
            titles[attempt.Id],
            attempt.Mode,
            Utc(attempt.StartedAt),
            EndsAt(attempt),
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
    /// reach; or the error that stops UC-24 to UC-29.
    /// </summary>
    private async Task<(Attempt? Attempt, Error? Refusal)> LoadRunningAsync(int id, CancellationToken ct)
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

        // E1: the access ended while the attempt was running; what is stored stays.
        var reachable = await registrations.ListAccessibleSubjectsAsync(userId, VietnamTime.Today(DateTime.UtcNow), ct);
        if (reachable.All(s => s.Id != attempt.SubjectId))
        {
            return (null, new Error(ErrorType.Forbidden, "Attempt.AccessEnded",
                $"Your access to {attempt.Subject.Name} has ended, so this attempt cannot continue. Your answers so far are kept."));
        }

        return (attempt, null);
    }

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

    /// <summary>BR-06: a simulation exam ends its duration after it started.</summary>
    internal static DateTime? EndsAt(Attempt attempt) =>
        attempt is { Mode: AttemptMode.Exam, Quiz: { } quiz } ? Utc(attempt.StartedAt).AddMinutes(quiz.DurationMin) : null;

    internal static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
