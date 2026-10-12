using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Admin;

/// <summary>UC-64 View Settings List, UC-65 Add or Edit Setting.</summary>
public interface ISettingService
{
    Task<Result<Page<SettingSummaryDto>>> ListAsync(SettingListRequest request, CancellationToken ct = default);

    Task<Result<SettingDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>UC-65: a new value of a type the code does not read by value.</summary>
    Task<Result<SettingSavedDto>> CreateAsync(CreateSettingRequest request, CancellationToken ct = default);

    /// <summary>UC-65: value, order, description and status; 409 Setting.Changed (E2), Setting.InUse (UC-66 E1).</summary>
    Task<Result<SettingSavedDto>> UpdateAsync(int id, UpdateSettingRequest request, CancellationToken ct = default);
}
