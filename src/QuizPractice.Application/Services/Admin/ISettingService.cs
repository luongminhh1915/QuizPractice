using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Admin;

/// <summary>UC-64 View Settings List.</summary>
public interface ISettingService
{
    Task<Result<Page<SettingSummaryDto>>> ListAsync(SettingListRequest request, CancellationToken ct = default);
}
