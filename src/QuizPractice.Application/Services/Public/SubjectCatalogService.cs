using FluentValidation;
using QuizPractice.Application.Abstractions;
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
    IHtmlSanitizer sanitizer,
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

    public async Task<Result<SubjectDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        // An unpublished subject answers like a missing one (E1).
        var subject = await subjects.GetPublishedAsync(id, ct);
        if (subject is null)
        {
            return new Error(ErrorType.NotFound, "Subject.NotAvailable", "This subject is not available.");
        }

        // The description is HTML. It is cleaned here, on the way out, so the public page is
        // safe whatever wrote it: the seed today, Subject Details (UC-44) later.
        return SubjectDetailsDto.From(subject) with { Description = sanitizer.Sanitize(subject.Description) };
    }

    public async Task<SubjectSiderDto> GetSiderAsync(CancellationToken ct = default)
    {
        var categories = await settings.ListActiveAsync(SettingType.SubjectCategory, ct);
        var featured = await subjects.ListFeaturedPublishedAsync(SiderFeaturedCount, ct);
        return new SubjectSiderDto(categories.Select(CategoryDto.From).ToList(), featured.Select(SubjectCardDto.From).ToList());
    }
}
