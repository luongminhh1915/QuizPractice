using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>
/// Criteria of Settings List (UC-64): text matched against the value. SortBy: id, type,
/// value, order or status; none keeps each type together in its display order.
/// </summary>
public sealed record SettingSearch(string? Text, SettingType? Type, ActiveStatus? Status, string? SortBy, bool Descending);

/// <summary>Values of the system settings (BR-16).</summary>
public interface ISettingRepository
{
    /// <summary>The active values of one type, in display order: what a menu or a drop-down offers.</summary>
    Task<IReadOnlyList<Setting>> ListActiveAsync(SettingType type, CancellationToken ct = default);

    /// <summary>One setting whatever its status, tracked so a record can refer to it or the Admin change it.</summary>
    Task<Setting?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>UC-64: a page of settings of every type and status.</summary>
    Task<Page<Setting>> SearchAsync(SettingSearch search, PageRequest page, CancellationToken ct = default);
}
