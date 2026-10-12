using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>Values of the system settings (BR-16).</summary>
public interface ISettingRepository
{
    /// <summary>The active values of one type, in display order: what a menu or a drop-down offers.</summary>
    Task<IReadOnlyList<Setting>> ListActiveAsync(SettingType type, CancellationToken ct = default);

    /// <summary>One setting whatever its status, tracked so a record can refer to it or the Admin change it.</summary>
    Task<Setting?> GetByIdAsync(int id, CancellationToken ct = default);
}
