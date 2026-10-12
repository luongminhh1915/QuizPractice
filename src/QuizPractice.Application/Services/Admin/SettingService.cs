using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Admin;

public class SettingService(
    ISettingRepository settings,
    ICurrentUser currentUser,
    IValidator<SettingListRequest> listValidator) : ISettingService
{
    public async Task<Result<Page<SettingSummaryDto>>> ListAsync(SettingListRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var search = new SettingSearch(request.Search, request.Type, request.Status, request.SortBy, request.Descending);
        var page = await settings.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(SettingSummaryDto.From);
    }
}
