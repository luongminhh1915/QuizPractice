using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Public;

/// <summary>UC-12 View Subjects List, UC-13 View Subject Details: published subjects only.</summary>
public interface ISubjectCatalogService
{
    Task<Result<Page<SubjectCardDto>>> ListAsync(SubjectListRequest request, CancellationToken ct = default);

    /// <summary>UC-13. E1: 404 when the subject does not exist or is no longer published.</summary>
    Task<Result<SubjectDetailsDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>The sider both subject pages share.</summary>
    Task<SubjectSiderDto> GetSiderAsync(CancellationToken ct = default);
}
