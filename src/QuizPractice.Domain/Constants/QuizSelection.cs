namespace QuizPractice.Domain.Constants;

/// <summary>
/// Quiz.SelectionType: how a quiz draws its questions (UC-57 step 5). "Topic", or the
/// value of a Dimension Type setting of the subject, such as "Domain" or "Group".
/// </summary>
public static class QuizSelection
{
    /// <summary>One group per topic of the subject: the questions of the lessons under it.</summary>
    public const string Topic = "Topic";
}
