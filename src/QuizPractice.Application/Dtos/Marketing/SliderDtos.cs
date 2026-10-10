using QuizPractice.Application.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Marketing;

/// <summary>Criteria of Sliders List (UC-38). SortBy: id, title, backlink or status.</summary>
public sealed record SliderListRequest(
    string? Search = null,
    VisibilityStatus? Status = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 10);

/// <summary>
/// Slider Details form (UC-39). Image is required for a new slider; on edit, leaving
/// it empty keeps the stored image. A new slider starts Hidden (BR-23).
/// </summary>
public sealed record SaveSliderRequest(
    string Title,
    string? Backlink,
    VisibilityStatus Status = VisibilityStatus.Hidden,
    string? Notes = null,
    FileUpload? Image = null);

/// <summary>Show or Hide Slider (UC-40).</summary>
public sealed record ChangeSliderStatusRequest(VisibilityStatus Status);

public sealed record SliderDto(
    int Id,
    string Title,
    string Image,
    string? Backlink,
    int DisplayOrder,
    string? Notes,
    VisibilityStatus Status)
{
    public static SliderDto From(Slider s) => new(s.Id, s.Title, s.Image, s.Backlink, s.DisplayOrder, s.Notes, s.Status);
}
