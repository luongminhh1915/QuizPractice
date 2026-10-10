using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>Criteria of Sliders List (UC-38): text matched against title and backlink, status filter.</summary>
public sealed record SliderSearch(string? Text, VisibilityStatus? Status, string? SortBy, bool Descending);

public interface ISliderRepository
{
    Task<Slider?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Page<Slider>> SearchAsync(SliderSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>Order a new slider takes: after the last one.</summary>
    Task<int> NextDisplayOrderAsync(CancellationToken ct = default);

    /// <summary>Stages a new slider; the service commits through IUnitOfWork.</summary>
    void Add(Slider slider);
}
