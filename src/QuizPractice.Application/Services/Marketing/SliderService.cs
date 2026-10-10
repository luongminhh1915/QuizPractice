using FluentValidation;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Marketing;

/// <summary>
/// The sample feature of the skeleton: copy its shape for every other list + details
/// pair. Validate, load through the repository, change the entity, commit once.
/// </summary>
public class SliderService(
    ISliderRepository sliders,
    IValidator<SliderListRequest> listValidator) : ISliderService
{
    public async Task<Result<Page<SliderDto>>> ListAsync(SliderListRequest request, CancellationToken ct = default)
    {
        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var search = new SliderSearch(request.Search, request.Status, request.SortBy, request.Descending);
        var page = await sliders.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(SliderDto.From);
    }
}
