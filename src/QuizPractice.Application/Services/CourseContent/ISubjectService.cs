using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.CourseContent;

/// <summary>UC-41 View Subject Management List, UC-42 Create Subject.</summary>
public interface ISubjectService
{
    /// <summary>Every subject for the Admin, the assigned ones for an Expert (BR-11).</summary>
    Task<Result<Page<SubjectSummaryDto>>> ListAsync(SubjectListRequest request, CancellationToken ct = default);

    Task<Result<SubjectFiltersDto>> GetFiltersAsync(CancellationToken ct = default);

    /// <summary>Subject categories, and the Experts an Admin may choose as owner.</summary>
    Task<Result<SubjectChoicesDto>> GetChoicesAsync(CancellationToken ct = default);

    /// <summary>The Overview tab; an Expert only for an assigned subject (UC-44 E1).</summary>
    Task<Result<SubjectDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>UC-42: the Admin creates an Unpublished subject, with or without an owner.</summary>
    Task<Result<SubjectDto>> CreateAsync(CreateSubjectRequest request, CancellationToken ct = default);
}
