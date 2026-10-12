using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Learning;

/// <summary>
/// The name of an attempt on Quiz Handle, Quiz Review and the lists: the exam name, or
/// the criteria of a practice (SRS III 3.2, 4.1: "by topic or by dimension, and the group").
/// </summary>
internal static class AttemptLabels
{
    public static async Task<IReadOnlyDictionary<int, string>> TitlesAsync(
        IReadOnlyCollection<Attempt> attempts, IAttemptRepository repository, CancellationToken ct)
    {
        var practices = attempts.Where(a => a.Mode == AttemptMode.Practice).ToList();
        var topics = await repository.GetLessonNamesAsync(GroupIds(practices, SelectionType.Topic), ct);
        var dimensions = await repository.GetDimensionNamesAsync(GroupIds(practices, SelectionType.Dimension), ct);
        return attempts.ToDictionary(a => a.Id, a => Title(a, topics, dimensions));
    }

    private static string Title(Attempt a, IReadOnlyDictionary<int, string> topics, IReadOnlyDictionary<int, string> dimensions)
    {
        if (a.Mode == AttemptMode.Exam)
        {
            return a.Quiz?.Name ?? "Simulation exam";
        }

        var byTopic = a.SelectionType == SelectionType.Topic;
        var names = byTopic ? topics : dimensions;
        var group = a.GroupId is { } id
            ? names.GetValueOrDefault(id, $"#{id}")
            : byTopic ? "all topics" : "all dimensions";
        return $"Practice by {(byTopic ? "topic" : "dimension")}: {group}";
    }

    private static List<int> GroupIds(IEnumerable<Attempt> practices, SelectionType type) =>
        practices.Where(a => a.SelectionType == type && a.GroupId is not null).Select(a => a.GroupId!.Value).Distinct().ToList();
}
