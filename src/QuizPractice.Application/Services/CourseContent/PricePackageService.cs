using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.CourseContent;

public class PricePackageService(
    IPricePackageRepository packages,
    ISubjectRepository subjects,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<SavePricePackageRequest> saveValidator) : IPricePackageService
{
    public async Task<Result<PricePackageTabDto>> ListAsync(int subjectId, CancellationToken ct = default)
    {
        if (await CheckAdminAsync(subjectId, ct) is { } denied)
        {
            return denied;
        }

        var stored = await packages.ListAsync(subjectId, ct);
        return new PricePackageTabDto(CanEdit: true, stored.Select(PricePackageDto.From).ToList());
    }

    public async Task<Result<PricePackageDto>> CreateAsync(int subjectId, SavePricePackageRequest request, CancellationToken ct = default)
    {
        if (await CheckAdminAsync(subjectId, ct) is { } denied)
        {
            return denied;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var package = new PricePackage { SubjectId = subjectId, Status = ActiveStatus.Active };
        Apply(package, request);
        packages.Add(package);
        await unitOfWork.SaveChangesAsync(ct);
        return PricePackageDto.From(package);
    }

    public async Task<Result<PricePackageDto>> UpdateAsync(int subjectId, int packageId, SavePricePackageRequest request, CancellationToken ct = default)
    {
        if (await CheckAdminAsync(subjectId, ct) is { } denied)
        {
            return denied;
        }

        var package = await packages.GetByIdAsync(packageId, ct);
        if (package is null || package.SubjectId != subjectId)
        {
            return Error.NotFound("PricePackage", packageId);
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // A registration stores its own total cost and validity, so a new price or
        // duration applies to new registrations only.
        Apply(package, request);
        await unitOfWork.SaveChangesAsync(ct);
        return PricePackageDto.From(package);
    }

    public async Task<Result<PricePackageDto>> ChangeStatusAsync(
        int subjectId, int packageId, ChangePricePackageStatusRequest request, CancellationToken ct = default)
    {
        if (await CheckAdminAsync(subjectId, ct) is { } denied)
        {
            return denied;
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Error.Invalid(nameof(request.Status), "Status must be Active or Inactive.");
        }

        var package = await packages.GetByIdAsync(packageId, ct);
        if (package is null || package.SubjectId != subjectId)
        {
            return Error.NotFound("PricePackage", packageId);
        }

        // Already in the chosen state: nothing is written.
        if (package.Status != request.Status)
        {
            package.Status = request.Status;
            await unitOfWork.SaveChangesAsync(ct);
        }

        return PricePackageDto.From(package);
    }

    /// <summary>F#27, BR-12: the packages are the Admin's; the Expert only views them (UC-48 step 4).</summary>
    private async Task<Error?> CheckAdminAsync(int subjectId, CancellationToken ct)
    {
        if (CourseAccess.CheckAdmin(currentUser) is { } denied)
        {
            return denied;
        }

        return await subjects.GetByIdAsync(subjectId, ct) is null ? Error.NotFound("Subject", subjectId) : null;
    }

    private static void Apply(PricePackage package, SavePricePackageRequest request)
    {
        package.Name = request.Name.Trim();
        package.AccessMonths = (byte)request.AccessMonths;
        package.ListPrice = request.ListPrice;
        package.SalePrice = request.SalePrice;
        package.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
    }
}
