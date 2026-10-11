using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Public;

namespace QuizPractice.Application.Services.Public;

/// <summary>UC-14 Register Subject: a signed-in customer only (OI-15).</summary>
public interface ISubjectRegisterService
{
    /// <summary>The packages, the contact from the profile and what blocks a registration.</summary>
    Task<Result<SubjectRegisterFormDto>> GetFormAsync(int subjectId, CancellationToken ct = default);

    /// <summary>Stores the registration as Submitted at the sale price of the chosen package.</summary>
    Task<Result<SubjectRegisteredDto>> RegisterAsync(int subjectId, SubjectRegisterRequest request, CancellationToken ct = default);
}
