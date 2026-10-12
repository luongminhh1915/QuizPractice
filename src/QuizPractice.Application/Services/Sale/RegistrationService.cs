using FluentValidation;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Sale;

/// <summary>
/// The Sale role sees every registration, online or recorded by any Sale user (F#35). The
/// controller requires the Registrations menu permission (D23), so only Sale reaches it.
/// </summary>
public class RegistrationService(
    IRegistrationRepository registrations,
    IQuizRepository quizzes,
    IValidator<RegistrationListRequest> listValidator) : IRegistrationService
{
    public async Task<Result<Page<RegistrationSummaryDto>>> ListAsync(RegistrationListRequest request, CancellationToken ct = default)
    {
        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // Vietnam days, both ends included: To covers its whole day.
        var search = new RegistrationSearch(
            request.Search,
            request.SubjectId,
            request.From is { } from ? VietnamTime.StartUtc(from) : null,
            request.To is { } to ? VietnamTime.StartUtc(to.AddDays(1)) : null,
            request.Status,
            request.SortBy,
            request.Descending);
        var page = await registrations.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(RegistrationSummaryDto.From);
    }

    public async Task<Result<RegistrationFiltersDto>> GetFiltersAsync(CancellationToken ct = default)
    {
        var subjects = await quizzes.GetSubjectsAsync(expertId: null, ct);
        return new RegistrationFiltersDto(subjects.Select(s => new SaleSubjectDto(s.Id, s.Name)).ToList());
    }
}
