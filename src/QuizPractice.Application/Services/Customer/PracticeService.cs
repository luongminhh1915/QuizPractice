using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Customer;

/// <summary>
/// A practice draws its questions at random among the shown questions of one group of the
/// subject, or of the whole subject (OI-06, D3). A topic group holds the questions of the
/// lessons under the Subject Topic and those attached to the topic itself, as the rules of
/// a quiz count them (UC-57; Duck 12/10).
/// </summary>
public class PracticeService(
    IRegistrationRepository registrations,
    IQuizRepository quizzes,
    IAttemptRepository attempts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<CreatePracticeRequest> createValidator) : IPracticeService
{
    public async Task<Result<PracticeFormDto>> GetFormAsync(CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        // E1 is an empty list: the screen says so and offers Subjects List.
        var subjects = await AccessibleSubjectsAsync(userId, ct);
        return new PracticeFormDto(subjects.Select(s => new PracticeSubjectDto(s.Id, s.Name)).ToList());
    }

    public async Task<Result<PracticeGroupsDto>> GetGroupsAsync(int subjectId, SelectionType selectedBy, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        if (!Enum.IsDefined(selectedBy))
        {
            return Error.Invalid("SelectedBy", "Choose by topic or by dimension.");
        }

        if ((await AccessibleSubjectsAsync(userId, ct)).All(s => s.Id != subjectId))
        {
            return CustomerAccess.NoAccess;
        }

        var groups = await LoadGroupsAsync(subjectId, selectedBy, ct);
        var total = (await attempts.GetShownQuestionIdsAsync(subjectId, ct)).Count;
        return new PracticeGroupsDto(total, groups.Items.Select(g => g with { Available = Count(g.Id) }).ToList());

        int Count(int groupId) => groups.Members.Where(m => m.GroupId == groupId).Select(m => m.QuestionId).Distinct().Count();
    }

    public async Task<Result<AttemptCreatedDto>> CreateAsync(CreatePracticeRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        if (await createValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // BR-05: the access may have ended since the form was loaded.
        if ((await AccessibleSubjectsAsync(userId, ct)).All(s => s.Id != request.SubjectId))
        {
            return CustomerAccess.NoAccess;
        }

        IReadOnlyList<int> pool;
        if (request.GroupId is { } groupId)
        {
            var groups = await LoadGroupsAsync(request.SubjectId, request.SelectionType, ct);
            if (groups.Items.All(g => g.Id != groupId))
            {
                return Error.Invalid(nameof(request.GroupId), "This group does not belong to the subject; choose the group again.");
            }

            pool = groups.Members.Where(m => m.GroupId == groupId).Select(m => m.QuestionId).Distinct().ToList();
        }
        else
        {
            pool = await attempts.GetShownQuestionIdsAsync(request.SubjectId, ct);
        }

        // E2: fewer questions match than asked; nothing is created (A2: lower the number).
        if (pool.Count < request.QuestionCount)
        {
            return Error.Invalid(nameof(request.QuestionCount), pool.Count == 0
                ? "No question matches this choice yet. Choose another group."
                : $"Only {pool.Count} questions match this choice. Lower the number to {pool.Count} or fewer.");
        }

        // Step 5: a random set, in a random order.
        var drawn = pool.ToArray();
        Random.Shared.Shuffle(drawn);
        var attempt = new Attempt
        {
            UserId = userId,
            SubjectId = request.SubjectId,
            Mode = AttemptMode.Practice,
            SelectionType = request.SelectionType,
            GroupId = request.GroupId,
            QuestionCount = (short)request.QuestionCount,
            StartedAt = DateTime.UtcNow,
            Status = AttemptStatus.InProgress,
        };
        for (var i = 0; i < request.QuestionCount; i++)
        {
            attempt.Questions.Add(new AttemptQuestion { Attempt = attempt, QuestionId = drawn[i], DisplayOrder = (short)(i + 1) });
        }

        attempts.Add(attempt);
        await unitOfWork.SaveChangesAsync(ct);
        return new AttemptCreatedDto(attempt.Id);
    }

    private Task<IReadOnlyList<Subject>> AccessibleSubjectsAsync(int userId, CancellationToken ct) =>
        registrations.ListAccessibleSubjectsAsync(userId, VietnamTime.Today(DateTime.UtcNow), ct);

    /// <summary>The topics of the subject, or every dimension of it (by type), and the shown questions of each.</summary>
    private async Task<Groups> LoadGroupsAsync(int subjectId, SelectionType selectedBy, CancellationToken ct)
    {
        if (selectedBy == SelectionType.Topic)
        {
            var topics = await quizzes.GetTopicsAsync(subjectId, ct);
            return new Groups(topics.Select(t => new PracticeGroupDto(t.Id, t.Name, null, 0)).ToList(),
                await quizzes.GetTopicMembersAsync(subjectId, ct));
        }

        var dimensions = await quizzes.GetDimensionsAsync(subjectId, ct);
        return new Groups(dimensions.Select(d => new PracticeGroupDto(d.Id, d.Name, d.Type.Value, 0)).ToList(),
            await quizzes.GetDimensionMembersAsync(subjectId, ct));
    }

    private sealed record Groups(IReadOnlyList<PracticeGroupDto> Items, IReadOnlyList<GroupMember> Members);
}
