namespace QuizPractice.Application.Dtos.Customer;

/// <summary>Criteria of Simulation Exams (UC-22): exam name, one subject; id, subject, name and level sort the list.</summary>
public sealed record ExamListRequest(string? Search = null, int? SubjectId = null, string? SortBy = null, bool Descending = false, int Page = 1, int PageSize = 10);

/// <summary>One row of Simulation Exams (SRS III 3.4).</summary>
public sealed record ExamSummaryDto(int Id, string SubjectName, string Name, string Level, int QuestionCount, int DurationMin, int PassRate);

/// <summary>The Subject filter: the subjects the customer may reach today. Empty is E2.</summary>
public sealed record ExamFiltersDto(IReadOnlyList<PracticeSubjectDto> Subjects);
