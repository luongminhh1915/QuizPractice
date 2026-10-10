namespace QuizPractice.Application.Dtos.TestContent;

/// <summary>A row the import refused: its line in the file and every reason (UC-55 E3).</summary>
public sealed record RejectedRowDto(int Line, IReadOnlyList<string> Reasons);

/// <summary>
/// The answer of Question Import (UC-55 step 6): rows read, rows stored as Hidden questions
/// in the subject, and the rejected rows. Nothing is stored for a rejected row.
/// </summary>
public sealed record QuestionImportResultDto(
    int SubjectId,
    string Subject,
    int RowsRead,
    int Imported,
    IReadOnlyList<RejectedRowDto> Rejected);
