using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>
/// Criteria of Settings List (UC-64): text matched against the value. SortBy: id, type,
/// value, order or status; none keeps each type together in its display order.
/// </summary>
/// <summary>Stored records that refer to a setting: what they are and how many (UC-66 E1).</summary>
public sealed record SettingUse(string What, int Count);

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

    /// <summary>Whether another setting of the type has this value (the match ignores case, as the unique index does).</summary>
    Task<bool> ExistsAsync(SettingType type, string value, int? exceptId, CancellationToken ct = default);

    /// <summary>The records that refer to this setting, by kind; empty when nothing does.</summary>
    Task<IReadOnlyList<SettingUse>> GetUseAsync(Setting setting, CancellationToken ct = default);

    /// <summary>The position after the last value of the type.</summary>
    Task<int> NextOrderAsync(SettingType type, CancellationToken ct = default);

    void Add(Setting setting);
}
