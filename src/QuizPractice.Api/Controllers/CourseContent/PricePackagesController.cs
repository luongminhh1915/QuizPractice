using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Services.CourseContent;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.CourseContent;

/// <summary>
/// Price Package tab of Subject Management Details (PDF II.6, F#27; UC-47, UC-48). GET for the
/// Admin and the assigned Expert; every change for the Admin only (403 for an Expert).
/// </summary>
[Route("api/subjects/{subjectId:int}/packages")]
[Authorize(Policy = SettingValues.MenuItems.Subjects)]
public class PricePackagesController(IPricePackageService packages) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PricePackageTabDto>> List(int subjectId, CancellationToken ct) =>
        FromResult(await packages.ListAsync(subjectId, ct));

    [HttpPost]
    public async Task<ActionResult<PricePackageDto>> Create(int subjectId, SavePricePackageRequest request, CancellationToken ct)
    {
        var result = await packages.CreateAsync(subjectId, request, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PricePackageDto>> Update(int subjectId, int id, SavePricePackageRequest request, CancellationToken ct) =>
        FromResult(await packages.UpdateAsync(subjectId, id, request, ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<PricePackageDto>> ChangeStatus(
        int subjectId, int id, ChangePricePackageStatusRequest request, CancellationToken ct) =>
        FromResult(await packages.ChangeStatusAsync(subjectId, id, request, ct));
}
