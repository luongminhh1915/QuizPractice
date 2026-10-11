using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.TestContent;

public class QuizService(
    IQuizRepository quizzes,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<SaveQuizRequest> saveValidator,
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

    public async Task<Result<QuizDto>> GetAsync(int id, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var quiz = await quizzes.GetWithRulesAsync(id, ct);
        if (quiz is null)
        {
            return Error.NotFound("Quiz", id);
        }

        // E1: an Expert opens a quiz of a subject not assigned to that Expert.
        if (expertId is not null && quiz.Subject.ExpertId != expertId)
        {
            return ContentAccess.NotAssigned;
        }

        return await ToDtoAsync(quiz, ct);
    }

    public async Task<Result<QuizChoicesDto>> GetChoicesAsync(CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var subjects = await quizzes.GetSubjectsAsync(expertId, ct);
        var levels = await quizzes.GetLevelsAsync(ct);
        var types = await quizzes.GetTypesAsync(ct);
        return new QuizChoicesDto(
            subjects.Select(s => new QuizSubjectDto(s.Id, s.Name, s.PassRate)).ToList(),
            levels.Where(l => l.Status == ActiveStatus.Active).Select(l => new QuestionLookupDto(l.Id, l.Value)).ToList(),
            types.Where(t => t.Status == ActiveStatus.Active).Select(t => new QuestionLookupDto(t.Id, t.Value)).ToList());
    }

    public async Task<Result<QuizGroupsDto>> GetGroupsAsync(int subjectId, string? drawnBy, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        if ((await quizzes.GetSubjectsAsync(expertId, ct)).All(s => s.Id != subjectId))
        {
            return expertId is null ? Error.NotFound("Subject", subjectId) : ContentAccess.NotAssigned;
        }

        var ways = await DrawnByAsync(subjectId, ct);
        var way = drawnBy ?? QuizSelection.Topic;
        var groups = ways.Contains(way) ? await LoadGroupsAsync(subjectId, way, ct) : null;
        if (groups is null)
        {
            return Error.Invalid("DrawnBy", $"Draw the questions by one of: {string.Join(", ", ways)}.");
        }

        return new QuizGroupsDto(ways, groups.All.Select(g => new QuizGroupDto(g.Id, g.Name, groups.Available(g.Id))).ToList());
    }

    public async Task<Result<QuizDto>> CreateAsync(SaveQuizRequest request, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var (type, badOverview) = await CheckOverviewAsync(request, expertId, keptLevelId: null, keptTypeId: null, ct);
        if (badOverview is not null)
        {
            return badOverview;
        }

        var groups = await LoadGroupsAsync(request.SubjectId, request.SelectionType, ct);
        if (CheckRules(request, groups) is { } badRules)
        {
            return badRules;
        }

        var quiz = new Quiz
        {
            SubjectId = request.SubjectId,
            LevelId = request.LevelId,
            TypeId = request.TypeId,
            Name = request.Name.Trim(),
            Description = QuestionMapping.NullIfBlank(request.Description),
            DurationMin = (short)request.DurationMin,
            PassRate = (byte)request.PassRate,
            QuestionCount = (short)request.QuestionCount,
            SelectionType = request.SelectionType,
            Status = request.Status,
        };
        foreach (var rule in request.Rules)
        {
            quiz.Rules.Add(new QuizRule { Quiz = quiz, GroupId = rule.GroupId, QuestionCount = (short)rule.QuestionCount });
        }

        // UC-57 step 7 (D13, OI-07): a lesson quiz draws its set once, now.
        if (IsLessonQuiz(type!))
        {
            var (drawn, shortOf) = Draw(request.Rules, groups!);
            if (drawn is null)
            {
                return Error.Invalid(nameof(request.Rules), shortOf!);
            }

            for (var i = 0; i < drawn.Count; i++)
            {
                quiz.Questions.Add(new QuizQuestion { Quiz = quiz, QuestionId = drawn[i], DisplayOrder = (short)(i + 1) });
            }
        }

        quizzes.Add(quiz);
        await unitOfWork.SaveChangesAsync(ct);
        return await ToDtoAsync((await quizzes.GetWithRulesAsync(quiz.Id, ct))!, ct);
    }

    public async Task<Result<QuizDto>> UpdateAsync(int id, SaveQuizRequest request, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var quiz = await quizzes.GetWithRulesAsync(id, ct);
        if (quiz is null)
        {
            return Error.NotFound("Quiz", id);
        }

        if (expertId is not null && quiz.Subject.ExpertId != expertId)
        {
            return ContentAccess.NotAssigned;
        }

        // A2 (BR-13): only the status of a taken quiz may change.
        if (await IsTakenAsync(id, ct))
        {
            return Taken(quiz);
        }

        var (type, badOverview) = await CheckOverviewAsync(request, expertId, quiz.LevelId, quiz.TypeId, ct);
        if (badOverview is not null)
        {
            return badOverview;
        }

        var groups = await LoadGroupsAsync(request.SubjectId, request.SelectionType, ct);
        if (CheckRules(request, groups) is { } badRules)
        {
            return badRules;
        }

        var lessonQuiz = IsLessonQuiz(type!);
        if (!lessonQuiz && IsLessonQuiz(quiz.Type) && await quizzes.GetLessonsUsingAsync(id, ct) is { Count: > 0 } lessons)
        {
            return Error.Conflict("Quiz.UsedByLesson",
                $"Lesson {string.Join(", ", lessons.Select(l => $"\"{l}\""))} opens this quiz, so it stays a lesson quiz.");
        }

        // A lesson quiz draws again only when what decides its set changes (Duck, 11/10).
        var storedRules = quiz.Rules.ToDictionary(r => r.GroupId, r => (int)r.QuestionCount);
        var settingChanged = quiz.SubjectId != request.SubjectId
            || quiz.SelectionType != request.SelectionType
            || quiz.QuestionCount != request.QuestionCount
            || !IsLessonQuiz(quiz.Type)
            || storedRules.Count != request.Rules.Count
            || request.Rules.Any(r => !storedRules.TryGetValue(r.GroupId, out var count) || count != r.QuestionCount);

        List<int>? drawn = null;
        if (lessonQuiz && (settingChanged || quiz.Questions.Count == 0))
        {
            (drawn, var shortOf) = Draw(request.Rules, groups!);
            if (drawn is null)
            {
                return Error.Invalid(nameof(request.Rules), shortOf!);
            }
        }

        quiz.SubjectId = request.SubjectId;
        quiz.LevelId = request.LevelId;
        quiz.TypeId = request.TypeId;
        quiz.Name = request.Name.Trim();
        quiz.Description = QuestionMapping.NullIfBlank(request.Description);
        quiz.DurationMin = (short)request.DurationMin;
        quiz.PassRate = (byte)request.PassRate;
        quiz.QuestionCount = (short)request.QuestionCount;
        quiz.SelectionType = request.SelectionType;
        quiz.Status = request.Status;

        // Rules by group: the key is (quiz, group), so a kept group is updated in place.
        foreach (var rule in quiz.Rules.Where(r => request.Rules.All(x => x.GroupId != r.GroupId)).ToList())
        {
            quizzes.RemoveRule(rule);
        }

        foreach (var input in request.Rules)
        {
            var rule = quiz.Rules.FirstOrDefault(r => r.GroupId == input.GroupId);
            if (rule is null)
            {
                quiz.Rules.Add(new QuizRule { Quiz = quiz, GroupId = input.GroupId, QuestionCount = (short)input.QuestionCount });
            }
            else
            {
                rule.QuestionCount = (short)input.QuestionCount;
            }
        }

        if (drawn is not null)
        {
            ReplaceFixedSet(quiz, drawn);
        }
        else if (!lessonQuiz)
        {
            ReplaceFixedSet(quiz, []);
        }

        // E5: the first attempt arrived while the user was editing.
        if (await IsTakenAsync(id, ct))
        {
            return Taken(quiz);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await ToDtoAsync((await quizzes.GetWithRulesAsync(id, ct))!, ct);
    }

    public async Task<Result<QuizDto>> ChangeStatusAsync(int id, ChangeQuizStatusRequest request, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Error.Invalid(nameof(request.Status), "Status must be Active or Inactive.");
        }

        var quiz = await quizzes.GetWithRulesAsync(id, ct);
        if (quiz is null)
        {
            return Error.NotFound("Quiz", id);
        }

        if (expertId is not null && quiz.Subject.ExpertId != expertId)
        {
            return ContentAccess.NotAssigned;
        }

        if (quiz.Status != request.Status)
        {
            quiz.Status = request.Status;
            await unitOfWork.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(quiz, ct);
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

    private async Task<QuizDto> ToDtoAsync(Quiz quiz, CancellationToken ct)
    {
        var groups = await LoadGroupsAsync(quiz.SubjectId, quiz.SelectionType, ct);
        var rules = quiz.Rules
            .Select(r => new QuizRuleDto(r.GroupId, groups?.NameOf(r.GroupId) ?? $"#{r.GroupId}", r.QuestionCount,
                groups?.Available(r.GroupId) ?? 0))
            .OrderBy(r => groups?.Order(r.GroupId) ?? r.GroupId)
            .ToList();
        return new QuizDto(quiz.Id, quiz.Name, quiz.SubjectId, quiz.Subject.Name, quiz.LevelId, quiz.TypeId, quiz.Type.Value,
            quiz.DurationMin, quiz.PassRate, quiz.Status, quiz.Description, quiz.QuestionCount, quiz.SelectionType, rules,
            quiz.Questions.Count, await IsTakenAsync(quiz.Id, ct));
    }

    private async Task<bool> IsTakenAsync(int id, CancellationToken ct) => (await quizzes.GetTakenAsync([id], ct)).Count > 0;

    private static Error Taken(Quiz quiz) =>
        Error.Conflict("Quiz.Taken",
            $"Customers have taken \"{quiz.Name}\", so only its status can change (BR-13). The quiz is reloaded read only.");

    private static bool IsLessonQuiz(Setting type) => type.Value == SettingValues.TestTypes.LessonQuiz;

    /// <summary>
    /// The subject must be reachable (E1), the level a Question Level and the type a Test
    /// Type; one no longer in use stays only on the quiz that already has it.
    /// </summary>
    private async Task<(Setting? Type, Error? Error)> CheckOverviewAsync(
        SaveQuizRequest request, int? expertId, int? keptLevelId, int? keptTypeId, CancellationToken ct)
    {
        if ((await quizzes.GetSubjectsAsync(expertId, ct)).All(s => s.Id != request.SubjectId))
        {
            return (null, expertId is null ? Error.Invalid(nameof(request.SubjectId), "This subject does not exist.") : ContentAccess.NotAssigned);
        }

        var level = (await quizzes.GetLevelsAsync(ct)).FirstOrDefault(l => l.Id == request.LevelId);
        if (level is null || (level.Status == ActiveStatus.Inactive && level.Id != keptLevelId))
        {
            return (null, Error.Invalid(nameof(request.LevelId), "Choose a level in use."));
        }

        var type = (await quizzes.GetTypesAsync(ct)).FirstOrDefault(t => t.Id == request.TypeId);
        if (type is null || (type.Status == ActiveStatus.Inactive && type.Id != keptTypeId))
        {
            return (null, Error.Invalid(nameof(request.TypeId), "Choose a quiz type in use."));
        }

        return (type, null);
    }

    /// <summary>UC-57 E2 and A4: the groups must belong to the subject and the way of drawing, and hold enough shown questions.</summary>
    private static Error? CheckRules(SaveQuizRequest request, Groups? groups)
    {
        if (groups is null)
        {
            return Error.Invalid(nameof(request.SelectionType), "Draw the questions by Topic or by a dimension type of the subject.");
        }

        if (request.Rules.Any(r => groups.All.All(g => g.Id != r.GroupId)))
        {
            return Error.Invalid(nameof(request.Rules),
                "A group does not belong to the chosen subject and way of drawing; choose the groups again.");
        }

        var shortGroups = request.Rules
            .Where(r => groups.Available(r.GroupId) < r.QuestionCount)
            .Select(r => $"{groups.NameOf(r.GroupId)} holds {groups.Available(r.GroupId)} shown questions, fewer than the {r.QuestionCount} asked.")
            .ToArray();
        return shortGroups.Length == 0
            ? null
            : Error.Invalid(new Dictionary<string, string[]> { [nameof(request.Rules)] = shortGroups });
    }

    /// <summary>
    /// The fixed set of a lesson quiz: at random within each group, a question never twice
    /// (one may sit in two dimension groups). Null with the reason when a group runs out.
    /// </summary>
    private static (List<int>? Drawn, string? ShortOf) Draw(IReadOnlyList<QuizRuleInput> rules, Groups groups)
    {
        var drawn = new List<int>();
        var used = new HashSet<int>();
        foreach (var rule in rules)
        {
            var pool = groups.Members.Where(m => m.GroupId == rule.GroupId && !used.Contains(m.QuestionId))
                .Select(m => m.QuestionId).Distinct().ToArray();
            if (pool.Length < rule.QuestionCount)
            {
                return (null, $"{groups.NameOf(rule.GroupId)} has only {pool.Length} questions not already drawn for another group; "
                              + $"lower its count to {pool.Length} or choose other groups.");
            }

            Random.Shared.Shuffle(pool);
            foreach (var questionId in pool.Take(rule.QuestionCount))
            {
                drawn.Add(questionId);
                used.Add(questionId);
            }
        }

        return (drawn, null);
    }

    /// <summary>Keeps the rows of questions drawn again (key quiz + question), removes the others, adds the new.</summary>
    private void ReplaceFixedSet(Quiz quiz, IReadOnlyList<int> drawn)
    {
        foreach (var row in quiz.Questions.Where(q => !drawn.Contains(q.QuestionId)).ToList())
        {
            quizzes.RemoveQuestion(row);
        }

        for (var i = 0; i < drawn.Count; i++)
        {
            var row = quiz.Questions.FirstOrDefault(q => q.QuestionId == drawn[i]);
            if (row is null)
            {
                quiz.Questions.Add(new QuizQuestion { Quiz = quiz, QuestionId = drawn[i], DisplayOrder = (short)(i + 1) });
            }
            else
            {
                row.DisplayOrder = (short)(i + 1);
            }
        }
    }

    /// <summary>"Topic" and the dimension types the subject has, in settings order.</summary>
    private async Task<IReadOnlyList<string>> DrawnByAsync(int subjectId, CancellationToken ct) =>
        [QuizSelection.Topic, .. (await quizzes.GetDimensionsAsync(subjectId, ct)).Select(d => d.Type.Value).Distinct()];

    /// <summary>The groups of one way of drawing and their shown questions; null when the subject has no such way.</summary>
    private async Task<Groups?> LoadGroupsAsync(int subjectId, string selectionType, CancellationToken ct)
    {
        if (selectionType == QuizSelection.Topic)
        {
            var topics = await quizzes.GetTopicsAsync(subjectId, ct);
            return new Groups(topics.Select(t => (t.Id, t.Name)).ToList(), await quizzes.GetTopicMembersAsync(subjectId, ct));
        }

        var dimensions = (await quizzes.GetDimensionsAsync(subjectId, ct)).Where(d => d.Type.Value == selectionType).ToList();
        return dimensions.Count == 0
            ? null
            : new Groups(dimensions.Select(d => (d.Id, d.Name)).ToList(), await quizzes.GetDimensionMembersAsync(subjectId, ct));
    }

    private sealed record Groups(IReadOnlyList<(int Id, string Name)> All, IReadOnlyList<GroupMember> Members)
    {
        public int Available(int groupId) => Members.Where(m => m.GroupId == groupId).Select(m => m.QuestionId).Distinct().Count();

        public string NameOf(int groupId) => All.FirstOrDefault(g => g.Id == groupId).Name ?? $"#{groupId}";

        public int Order(int groupId) => All.ToList().FindIndex(g => g.Id == groupId);
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
