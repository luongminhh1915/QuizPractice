using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>The groups of one way of drawing (topics, or the dimensions of one type) and their shown questions.</summary>
internal sealed record DrawGroups(IReadOnlyList<(int Id, string Name)> All, IReadOnlyList<GroupMember> Members)
{
    public int Available(int groupId) => Members.Where(m => m.GroupId == groupId).Select(m => m.QuestionId).Distinct().Count();

    public string NameOf(int groupId) => All.FirstOrDefault(g => g.Id == groupId).Name ?? $"#{groupId}";

    public int Order(int groupId) => All.ToList().FindIndex(g => g.Id == groupId);
}

/// <summary>
/// Drawing questions by the rules of a quiz (D13, OI-07): a lesson quiz once when it is
/// saved (UC-57), a simulation exam on every start (UC-23).
/// </summary>
internal static class QuestionDraw
{
    /// <summary>The groups of one way of drawing and their shown questions; null when the subject has no such way.</summary>
    public static async Task<DrawGroups?> LoadGroupsAsync(IQuizRepository quizzes, int subjectId, string selectionType, CancellationToken ct)
    {
        if (selectionType == QuizSelection.Topic)
        {
            var topics = await quizzes.GetTopicsAsync(subjectId, ct);
            return new DrawGroups(topics.Select(t => (t.Id, t.Name)).ToList(), await quizzes.GetTopicMembersAsync(subjectId, ct));
        }

        var dimensions = (await quizzes.GetDimensionsAsync(subjectId, ct)).Where(d => d.Type.Value == selectionType).ToList();
        return dimensions.Count == 0
            ? null
            : new DrawGroups(dimensions.Select(d => (d.Id, d.Name)).ToList(), await quizzes.GetDimensionMembersAsync(subjectId, ct));
    }

    /// <summary>
    /// At random within each group, a question never twice (one may sit in two dimension
    /// groups). Null with the reason when a group runs out.
    /// </summary>
    public static (List<int>? Drawn, string? ShortOf) Draw(IReadOnlyList<(int GroupId, int Count)> rules, DrawGroups groups)
    {
        var drawn = new List<int>();
        var used = new HashSet<int>();
        foreach (var (groupId, count) in rules)
        {
            var pool = groups.Members.Where(m => m.GroupId == groupId && !used.Contains(m.QuestionId))
                .Select(m => m.QuestionId).Distinct().ToArray();
            if (pool.Length < count)
            {
                return (null, $"{groups.NameOf(groupId)} has only {pool.Length} questions not already drawn for another group; "
                              + $"lower its count to {pool.Length} or choose other groups.");
            }

            Random.Shared.Shuffle(pool);
            foreach (var questionId in pool.Take(count))
            {
                drawn.Add(questionId);
                used.Add(questionId);
            }
        }

        return (drawn, null);
    }
}
