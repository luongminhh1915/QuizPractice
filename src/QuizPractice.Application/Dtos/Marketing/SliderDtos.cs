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
