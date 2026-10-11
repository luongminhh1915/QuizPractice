using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Public;

/// <summary>UC-12 View Subjects List, UC-13 View Subject Details: published subjects only.</summary>
public interface ISubjectCatalogService
{
    Task<Result<Page<SubjectCardDto>>> ListAsync(SubjectListRequest request, CancellationToken ct = default);

    /// <summary>The sider both subject pages share.</summary>
    Task<SubjectSiderDto> GetSiderAsync(CancellationToken ct = default);
}
