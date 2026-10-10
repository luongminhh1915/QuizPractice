namespace QuizPractice.Domain.Constants;

/// <summary>
/// Setting values the code has to recognise because behaviour depends on them.
/// Other values (categories, levels, dimension types) are plain data. The seed
/// writes these spellings; renaming one of them in Settings breaks the behaviour
/// that reads it.
/// </summary>
public static class SettingValues
{
    /// <summary>Lesson Type: what Lesson Details shows (UC-50).</summary>
    public static class LessonTypes
    {
        /// <summary>A chapter; other lessons sit under it (D12).</summary>
        public const string SubjectTopic = "Subject Topic";
        public const string Lesson = "Lesson";
        /// <summary>Points to a lesson quiz of the same subject.</summary>
        public const string Quiz = "Quiz";
    }

    /// <summary>Test Type: how a quiz draws its questions (D13).</summary>
    public static class TestTypes
    {
        /// <summary>Draws a new set on every attempt.</summary>
        public const string Simulation = "Simulation";
        /// <summary>Draws one fixed set when it is saved.</summary>
        public const string LessonQuiz = "Lesson quiz";
    }
}
