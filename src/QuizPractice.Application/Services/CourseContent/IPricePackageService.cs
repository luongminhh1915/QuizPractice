using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;

namespace QuizPractice.Application.Services.CourseContent;

/// <summary>UC-47 Add or Edit Price Package (Admin).</summary>
public interface IPricePackageService
{
    Task<Result<PricePackageTabDto>> ListAsync(int subjectId, CancellationToken ct = default);

    Task<Result<PricePackageDto>> CreateAsync(int subjectId, SavePricePackageRequest request, CancellationToken ct = default);

    Task<Result<PricePackageDto>> UpdateAsync(int subjectId, int packageId, SavePricePackageRequest request, CancellationToken ct = default);

    /// <summary>A2: registrations made under a package keep their access when it is deactivated.</summary>
    Task<Result<PricePackageDto>> ChangeStatusAsync(int subjectId, int packageId, ChangePricePackageStatusRequest request, CancellationToken ct = default);
}
