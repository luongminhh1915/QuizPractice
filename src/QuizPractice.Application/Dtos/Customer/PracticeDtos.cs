using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Customer;

/// <summary>A subject the customer may practise (BR-05).</summary>
public sealed record PracticeSubjectDto(int Id, string Name);

/// <summary>
/// Practice Details, new practice (SRS III 3.3; UC-20 step 2). An empty list is E1: the
/// customer has no subject with valid access, and the screen offers Subjects List.
/// </summary>
public sealed record PracticeFormDto(IReadOnlyList<PracticeSubjectDto> Subjects);

/// <summary>One topic or one dimension of the subject and the shown questions it holds. Type is the dimension type, empty for a topic.</summary>
public sealed record PracticeGroupDto(int Id, string Name, string? Type, int Available);

/// <summary>The groups of one way of selecting (Question Group) and the shown questions of the whole subject (all groups).</summary>
public sealed record PracticeGroupsDto(int Total, IReadOnlyList<PracticeGroupDto> Groups);

/// <summary>UC-20 step 3. GroupId empty = all topics, or all dimensions.</summary>
public sealed record CreatePracticeRequest(int SubjectId, int QuestionCount, SelectionType SelectionType, int? GroupId);

/// <summary>The attempt just made; Quiz Handle opens it.</summary>
public sealed record AttemptCreatedDto(int Id);
