using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Marketing;

/// <summary>
/// The sample feature of the skeleton: copy its shape for every other list + details
/// pair. Validate, load through the repository, change the entity, commit once.
/// </summary>
public class SliderService(
    ISliderRepository sliders,
    IUnitOfWork unitOfWork,
    IFileStorage files,
    IValidator<SaveSliderRequest> saveValidator,
    IValidator<SliderListRequest> listValidator) : ISliderService
{
    private const string ImageFolder = "sliders";

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

    public async Task<Result<SliderDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var slider = await sliders.GetByIdAsync(id, ct);
        return slider is null ? Error.NotFound("Slider", id) : SliderDto.From(slider);
    }

    public async Task<Result<SliderDto>> CreateAsync(SaveSliderRequest request, CancellationToken ct = default)
    {
        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        if (request.Image is null)
        {
            return Error.Invalid(nameof(request.Image), "An image is required for a new slider.");
        }

        var slider = new Slider
        {
            Title = request.Title.Trim(),
            Backlink = NullIfBlank(request.Backlink),
            Notes = NullIfBlank(request.Notes),
            Status = request.Status,
            DisplayOrder = await sliders.NextDisplayOrderAsync(ct),
            Image = await files.SaveAsync(request.Image.Content, request.Image.Extension, ImageFolder, ct),
        };
        sliders.Add(slider);
        await unitOfWork.SaveChangesAsync(ct);
        return SliderDto.From(slider);
    }

    public async Task<Result<SliderDto>> UpdateAsync(int id, SaveSliderRequest request, CancellationToken ct = default)
    {
        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var slider = await sliders.GetByIdAsync(id, ct);
        if (slider is null)
        {
            return Error.NotFound("Slider", id);
        }

        string? replacedImage = null;
        if (request.Image is not null)
        {
            replacedImage = slider.Image;
            slider.Image = await files.SaveAsync(request.Image.Content, request.Image.Extension, ImageFolder, ct);
        }

        slider.Title = request.Title.Trim();
        slider.Backlink = NullIfBlank(request.Backlink);
        slider.Notes = NullIfBlank(request.Notes);
        slider.Status = request.Status;
        await unitOfWork.SaveChangesAsync(ct);

        // Only after the commit: if it had failed, the row would still point at the old file.
        if (replacedImage is not null)
        {
            files.Delete(replacedImage);
        }

        return SliderDto.From(slider);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
