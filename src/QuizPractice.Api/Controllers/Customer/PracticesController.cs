using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Api.Controllers.Customer;

/// <summary>Practices List and Practice Details (F#14, F#15; UC-19 to UC-21). The customer's own practices only.</summary>
[Route("api/practices")]
[Authorize(Policy = SettingValues.MenuItems.Practices)]
public class PracticesController(IPracticeService practices) : ApiControllerBase
{
    /// <summary>UC-21: a practice or a simulation exam of the customer; 404 when missing or someone else's (E1).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PracticeDetailsDto>> Get(int id, CancellationToken ct) => FromResult(await practices.GetAsync(id, ct));

    /// <summary>UC-20: the subjects the customer may practise; empty for E1.</summary>
    [HttpGet("form")]
    public async Task<ActionResult<PracticeFormDto>> Form(CancellationToken ct) => FromResult(await practices.GetFormAsync(ct));

    /// <summary>UC-20: ?subjectId=&amp;selectedBy=Topic|Dimension. 403 Subject.NoAccess without valid access (BR-05).</summary>
    [HttpGet("groups")]
    public async Task<ActionResult<PracticeGroupsDto>> Groups([FromQuery] int subjectId, [FromQuery] SelectionType selectedBy, CancellationToken ct) =>
        FromResult(await practices.GetGroupsAsync(subjectId, selectedBy, ct));

    /// <summary>UC-20: 201 with the attempt Quiz Handle opens; 400 QuestionCount when fewer match (E2).</summary>
    [HttpPost]
    public async Task<ActionResult<AttemptCreatedDto>> Create(CreatePracticeRequest request, CancellationToken ct)
    {
        var result = await practices.CreateAsync(request, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }
}
