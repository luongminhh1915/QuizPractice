using System.Security.Cryptography;
using System.Text;
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

/// <summary>
/// A row of Settings List. Version: what the row was loaded with, so Activate / Deactivate
/// on a row another Admin changed is refused (UC-66 E2). CanDeactivate: false for a value
/// the system needs (roles, lesson and test types, the Settings menu item).
/// </summary>
public sealed record SettingSummaryDto(int Id, SettingType Type, string Value, int DisplayOrder, ActiveStatus Status, string Version, bool CanDeactivate)
{
    public static SettingSummaryDto From(Setting s, bool canDeactivate) =>
        new(s.Id, s.Type, s.Value, s.DisplayOrder, s.Status, SettingVersion.Of(s), canDeactivate);
}

/// <summary>Activate or Deactivate Setting (UC-66); ConfirmInUse answers E1.</summary>
public sealed record ChangeSettingStatusRequest(ActiveStatus Status, string? Version, bool ConfirmInUse = false);

/// <summary>New setting (UC-65 step 3): a type among the eight, then value, order, description and status.</summary>
public sealed record CreateSettingRequest(SettingType Type, string Value, int? DisplayOrder, string? Description, ActiveStatus Status);

/// <summary>
/// A stored setting keeps its type (D24). Version: the one the form was loaded with (E2).
/// ConfirmInUse: the Admin agreed to switch off a value records still use (UC-66 E1).
/// </summary>
public sealed record UpdateSettingRequest(
    string Value, int? DisplayOrder, string? Description, ActiveStatus Status, string? Version, bool ConfirmInUse = false);

/// <summary>
/// Setting Details. ValueLocked: the code reads this type by its values (roles, menu, lesson
/// and test types), so the value stays (Duck, 12/10). MenuRoles: who sees a System Menu item (A4).
/// </summary>
public sealed record SettingDto(
    int Id,
    SettingType Type,
    string Value,
    int DisplayOrder,
    string? Description,
    ActiveStatus Status,
    string Version,
    bool ValueLocked,
    bool CanDeactivate,
    IReadOnlyList<string> MenuRoles);

/// <summary>After a save; QuizzesFollowed: quizzes that now draw by the renamed dimension type.</summary>
public sealed record SettingSavedDto(SettingDto Setting, int QuizzesFollowed);

/// <summary>E2: a fingerprint of what the form shows, so a save on stale values is refused without a schema change.</summary>
public static class SettingVersion
{
    public static string Of(Setting s)
    {
        var text = string.Join('\u001f', s.Type, s.Value, s.DisplayOrder, s.Description ?? "", s.Status);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)), 0, 8);
    }
}
