using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Admin;

/// <summary>UC-62 View Users List.</summary>
public interface IUserService
{
    Task<Result<Page<UserSummaryDto>>> ListAsync(UserListRequest request, CancellationToken ct = default);

    Task<Result<UserChoicesDto>> GetChoicesAsync(CancellationToken ct = default);
}
