using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Services.CourseContent;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.CourseContent;

/// <summary>
/// Dimension tab of Subject Management Details (PDF II.6, F#26; UC-46). The Admin and the
/// Expert of the subject; another Expert gets 403 (E1).
/// </summary>
[Route("api/subjects/{subjectId:int}/dimensions")]
[Authorize(Policy = SettingValues.MenuItems.Subjects)]
public class SubjectDimensionsController(ISubjectDimensionService dimensions) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DimensionTabDto>> List(int subjectId, CancellationToken ct) =>
        FromResult(await dimensions.ListAsync(subjectId, ct));

    [HttpPost]
    public async Task<ActionResult<DimensionDto>> Create(int subjectId, SaveDimensionRequest request, CancellationToken ct)
    {
        var result = await dimensions.CreateAsync(subjectId, request, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }

    /// <summary>409 Dimension.TypeInUse when a quiz draws from it by its current type.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DimensionDto>> Update(int subjectId, int id, SaveDimensionRequest request, CancellationToken ct) =>
        FromResult(await dimensions.UpdateAsync(subjectId, id, request, ct));

    /// <summary>204 when removed; 409 Dimension.InUse with the counts (E3).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int subjectId, int id, CancellationToken ct)
    {
        var result = await dimensions.DeleteAsync(subjectId, id, ct);
        return result.IsSuccess ? NoContent() : Problem(result.Error!);
    }
}
