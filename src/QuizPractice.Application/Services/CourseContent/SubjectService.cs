using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.CourseContent;

public class SubjectService(
    ISubjectRepository subjects,
    ISettingRepository settings,
    ICurrentUser currentUser,
    IValidator<SubjectListRequest> listValidator) : ISubjectService
{
    public async Task<Result<Page<SubjectSummaryDto>>> ListAsync(SubjectListRequest request, CancellationToken ct = default)
    {
        if (CourseAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // Step 2: an Expert only ever receives the assigned subjects; none yet is an empty list (A4).
        var search = new ManagedSubjectSearch(
            expertId, request.Search, request.CategoryId, request.Status, request.SortBy, request.Descending);
        var page = await subjects.SearchManagedAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        var lessons = await subjects.CountLessonsAsync(page.Items.Select(s => s.Id).ToList(), ct);
        return page.Map(s => SubjectSummaryDto.From(s, lessons.GetValueOrDefault(s.Id)));
    }

    public async Task<Result<SubjectFiltersDto>> GetFiltersAsync(CancellationToken ct = default)
    {
        if (CourseAccess.Check(currentUser, out _) is { } denied)
        {
            return denied;
        }

        var categories = await settings.ListActiveAsync(SettingType.SubjectCategory, ct);
        return new SubjectFiltersDto(categories.Select(c => new LookupDto(c.Id, c.Value)).ToList());
    }
}
