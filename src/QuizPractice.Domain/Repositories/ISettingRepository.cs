using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>Values of the system settings (BR-16).</summary>
public interface ISettingRepository
{
    /// <summary>The active values of one type, in display order: what a menu or a drop-down offers.</summary>
    Task<IReadOnlyList<Setting>> ListActiveAsync(SettingType type, CancellationToken ct = default);
}
