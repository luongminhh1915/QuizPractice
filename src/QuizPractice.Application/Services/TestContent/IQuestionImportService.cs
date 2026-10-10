using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>UC-55 Import Questions.</summary>
public interface IQuestionImportService
{
    /// <summary>Stores the valid rows of the file as Hidden questions of the subject; reports the others.</summary>
    Task<Result<QuestionImportResultDto>> ImportAsync(int subjectId, FileUpload? file, CancellationToken ct = default);

    /// <summary>The sample template of the subject, with its lesson, dimension and level names (UC-55 step 2, A1).</summary>
    Task<Result<FileDownload>> GetTemplateAsync(int subjectId, CancellationToken ct = default);
}
