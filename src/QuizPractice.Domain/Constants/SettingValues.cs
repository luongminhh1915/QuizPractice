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

    /// <summary>
    /// System Menu: one item per group of screens. The permission table of
    /// appsettings.json gives each item its roles; the API guards its controllers with
    /// the same names and the menu shows the active items the role may open (D23, BR-03).
    /// </summary>
    public static class MenuItems
    {
        public const string Dashboard = "Dashboard";
        public const string Posts = "Posts";
        public const string Sliders = "Sliders";
        public const string Registrations = "Registrations";
        public const string Invoices = "Invoices";
        public const string Subjects = "Subjects";
        public const string Questions = "Questions";
        public const string Quizzes = "Quizzes";
        public const string Users = "Users";
        public const string Settings = "Settings";
        public const string MyRegistrations = "My Registrations";
        public const string Practices = "Practices";
        public const string SimulationExams = "Simulation Exams";
        public const string MyInvoices = "My Invoices";

        public static readonly IReadOnlyList<string> All =
        [
            Dashboard, Posts, Sliders, Registrations, Invoices, Subjects, Questions, Quizzes, Users, Settings,
            MyRegistrations, Practices, SimulationExams, MyInvoices,
        ];
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
