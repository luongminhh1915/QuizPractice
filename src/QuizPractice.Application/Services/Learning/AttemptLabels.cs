using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Learning;

/// <summary>Title of an attempt, and for a practice the group it drew from ("All topics" when every one).</summary>
internal sealed record AttemptLabel(string Title, string? Group);

/// <summary>
/// The name of an attempt on Quiz Handle, Quiz Review and the lists: the exam name, or
/// the criteria of a practice (SRS III 3.2, 4.1: "by topic or by dimension, and the group").
/// </summary>
internal static class AttemptLabels
{
    public static async Task<IReadOnlyDictionary<int, AttemptLabel>> LabelsAsync(
        IReadOnlyCollection<Attempt> attempts, IAttemptRepository repository, CancellationToken ct)
    {
        var practices = attempts.Where(a => a.Mode == AttemptMode.Practice).ToList();
        var topics = await repository.GetLessonNamesAsync(GroupIds(practices, SelectionType.Topic), ct);
        var dimensions = await repository.GetDimensionNamesAsync(GroupIds(practices, SelectionType.Dimension), ct);
        return attempts.ToDictionary(a => a.Id, a => Label(a, topics, dimensions));
    }

    private static AttemptLabel Label(Attempt a, IReadOnlyDictionary<int, string> topics, IReadOnlyDictionary<int, string> dimensions)
    {
        if (a.Mode == AttemptMode.Exam)
        {
            return new AttemptLabel(a.Quiz?.Name ?? "Simulation exam", null);
        }

        var byTopic = a.SelectionType == SelectionType.Topic;
        var names = byTopic ? topics : dimensions;
        var group = a.GroupId is { } id
            ? names.GetValueOrDefault(id, $"#{id}")
            : byTopic ? "All topics" : "All dimensions";
        return new AttemptLabel($"Practice by {(byTopic ? "topic" : "dimension")}: {group}", group);
    }

    private static List<int> GroupIds(IEnumerable<Attempt> practices, SelectionType type) =>
        practices.Where(a => a.SelectionType == type && a.GroupId is not null).Select(a => a.GroupId!.Value).Distinct().ToList();
}
