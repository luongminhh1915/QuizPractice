using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Marketing;

/// <summary>UC-38 View Sliders List, UC-39 Add or Edit Slider, UC-40 Show or Hide Slider.</summary>
public interface ISliderService
{
    Task<Result<Page<SliderDto>>> ListAsync(SliderListRequest request, CancellationToken ct = default);

    Task<Result<SliderDto>> GetAsync(int id, CancellationToken ct = default);

    Task<Result<SliderDto>> CreateAsync(SaveSliderRequest request, CancellationToken ct = default);

    Task<Result<SliderDto>> UpdateAsync(int id, SaveSliderRequest request, CancellationToken ct = default);

    Task<Result<SliderDto>> ChangeStatusAsync(int id, ChangeSliderStatusRequest request, CancellationToken ct = default);
}
