using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Services.Customer;

/// <summary>Practices List and Practice Details (F#14, F#15): the customer's own practices only.</summary>
public interface IPracticeService
{
    /// <summary>UC-19: the customer's practices and simulation exams, the newest first.</summary>
    Task<Result<Page<PracticeSummaryDto>>> ListAsync(PracticeListRequest request, CancellationToken ct = default);

    /// <summary>UC-19: the subjects to filter by.</summary>
    Task<Result<PracticeFiltersDto>> GetFiltersAsync(CancellationToken ct = default);

    /// <summary>UC-21: one attempt of the customer, read only, with its result once scored.</summary>
    Task<Result<PracticeDetailsDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>UC-20 step 2: the subjects the customer may practise today (BR-05).</summary>
    Task<Result<PracticeFormDto>> GetFormAsync(CancellationToken ct = default);

    /// <summary>UC-20 step 3: the topics or the dimensions of a subject, with how many shown questions each holds.</summary>
    Task<Result<PracticeGroupsDto>> GetGroupsAsync(int subjectId, SelectionType selectedBy, CancellationToken ct = default);

    /// <summary>UC-20 steps 4 and 5: draws the questions at random and starts the attempt.</summary>
    Task<Result<AttemptCreatedDto>> CreateAsync(CreatePracticeRequest request, CancellationToken ct = default);
}
