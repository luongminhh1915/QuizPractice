using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.CourseContent;

/// <summary>UC-41 View Subject Management List.</summary>
public interface ISubjectService
{
    /// <summary>Every subject for the Admin, the assigned ones for an Expert (BR-11).</summary>
    Task<Result<Page<SubjectSummaryDto>>> ListAsync(SubjectListRequest request, CancellationToken ct = default);

    Task<Result<SubjectFiltersDto>> GetFiltersAsync(CancellationToken ct = default);
}
