namespace QuizPractice.Application.Dtos.Customer;

/// <summary>One subject the customer may reach today (BR-05), as My Subjects lists it (UC-71).</summary>
public sealed record MySubjectDto(int Id, string Name, string Thumbnail, string? Brief);

/// <summary>The Active lessons of one subject, under their topics (UC-71, SRS III 3.6).</summary>
public sealed record SubjectLessonsDto(int SubjectId, string SubjectName, IReadOnlyList<LearnerTopicDto> Topics);

/// <summary>A Subject Topic and the lessons under it; Id and Name are empty for lessons outside any topic.</summary>
public sealed record LearnerTopicDto(int? Id, string? Name, IReadOnlyList<LearnerLessonDto> Lessons);

/// <summary>One lesson; Quiz is filled for a lesson of type Quiz.</summary>
public sealed record LearnerLessonDto(int Id, string Name, string Type, LessonQuizDto? Quiz);

/// <summary>
/// The lesson quiz a lesson points to. Available: Active, of type Lesson quiz and holding
/// its fixed set, so Take Quiz can start it. OpenAttemptId names the customer's unfinished
/// attempt at it: the page then offers Resume instead (UC-71 A1).
/// </summary>
public sealed record LessonQuizDto(int Id, string Name, int QuestionCount, int DurationMin, int PassRate, bool Available, int? OpenAttemptId);
