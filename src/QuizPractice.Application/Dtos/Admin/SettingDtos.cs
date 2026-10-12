using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Admin;

/// <summary>Criteria of Settings List (UC-64). SortBy: id, type, value, order or status.</summary>
public sealed record SettingListRequest(
    string? Search = null,
    SettingType? Type = null,
    ActiveStatus? Status = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 10);

/// <summary>A row of Settings List.</summary>
public sealed record SettingSummaryDto(int Id, SettingType Type, string Value, int DisplayOrder, ActiveStatus Status)
{
    public static SettingSummaryDto From(Setting s) => new(s.Id, s.Type, s.Value, s.DisplayOrder, s.Status);
}
