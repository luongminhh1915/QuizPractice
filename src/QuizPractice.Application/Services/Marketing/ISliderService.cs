using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Marketing;

/// <summary>UC-38 View Sliders List.</summary>
public interface ISliderService
{
    Task<Result<Page<SliderDto>>> ListAsync(SliderListRequest request, CancellationToken ct = default);
}
