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

    /// <summary>UC-22: the subjects the customer may reach; empty for E2.</summary>
    [HttpGet("filters")]
    public async Task<ActionResult<ExamFiltersDto>> Filters(CancellationToken ct) => FromResult(await exams.GetFiltersAsync(ct));
}
