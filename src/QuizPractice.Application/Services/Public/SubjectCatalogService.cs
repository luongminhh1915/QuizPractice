using FluentValidation;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Public;

/// <summary>
/// The public catalog. Each subject carries the prices of its cheapest active package
/// (UC-12 step 3); a subject without one shows no price and no Register (E2).
/// </summary>
public class SubjectCatalogService(
    ISubjectRepository subjects,
    ISettingRepository settings,
    IValidator<SubjectListRequest> listValidator) : ISubjectCatalogService
{
    public const int SiderFeaturedCount = 5;

    public async Task<Result<Page<SubjectCardDto>>> ListAsync(SubjectListRequest request, CancellationToken ct = default)
    {
        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // E1: no match is an empty page; the page keeps the criteria.
        var page = await subjects.SearchPublishedAsync(
            new PublishedSubjectSearch(request.Search, request.CategoryId), new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(SubjectCardDto.From);
    }

    public async Task<SubjectSiderDto> GetSiderAsync(CancellationToken ct = default)
    {
        var categories = await settings.ListActiveAsync(SettingType.SubjectCategory, ct);
        var featured = await subjects.ListFeaturedPublishedAsync(SiderFeaturedCount, ct);
        return new SubjectSiderDto(categories.Select(CategoryDto.From).ToList(), featured.Select(SubjectCardDto.From).ToList());
    }
}
