namespace QuizPractice.Domain.Enums;

/// <summary>The 8 fixed setting types (D24). A stored setting never changes type.</summary>
public enum SettingType
{
    UserRole,
    SystemMenu,
    PostCategory,
    SubjectCategory,
    TestType,
    QuestionLevel,
    LessonType,
    DimensionType,
}

/// <summary>Male or Female, as every form of SRS section III shows it (Duck 16/10).</summary>
public enum Gender { Male, Female }

/// <summary>Practice counts the clock up, Exam counts it down (BR-06).</summary>
public enum AttemptMode { Practice, Exam }

/// <summary>How a practice draws its questions (UC-20); empty for an exam.</summary>
public enum SelectionType { Topic, Dimension }
