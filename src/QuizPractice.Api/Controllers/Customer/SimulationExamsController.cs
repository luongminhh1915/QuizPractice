using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Customer;

/// <summary>Simulation Exams (F#16; UC-22, UC-23).</summary>
[Route("api/simulation-exams")]
[Authorize(Policy = SettingValues.MenuItems.SimulationExams)]
public class SimulationExamsController(ISimulationExamService exams) : ApiControllerBase
{
    /// <summary>UC-22: ?search=&amp;subjectId=&amp;sortBy=id|subject|name|level&amp;descending=true&amp;page=1.</summary>
    [HttpGet]
    public async Task<ActionResult<Page<ExamSummaryDto>>> List([FromQuery] ExamListRequest request, CancellationToken ct) =>
        FromResult(await exams.ListAsync(request, ct));

    /// <summary>UC-23: the exam details and the unfinished attempt at it (A2).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExamDetailsDto>> Get(int id, CancellationToken ct) => FromResult(await exams.GetAsync(id, ct));

    /// <summary>UC-23: 201 with the attempt; 403 Subject.NoAccess (E1), 409 Exam.Unfinished (A2) or Exam.NotAvailable (E2).</summary>
    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<AttemptCreatedDto>> Start(int id, CancellationToken ct)
    {
        var result = await exams.StartAsync(id, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }

    /// <summary>UC-22: the subjects the customer may reach; empty for E2.</summary>
    [HttpGet("filters")]
    public async Task<ActionResult<ExamFiltersDto>> Filters(CancellationToken ct) => FromResult(await exams.GetFiltersAsync(ct));
}
