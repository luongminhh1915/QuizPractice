using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.CourseContent;

public class SubjectDimensionService(
    ISubjectDimensionRepository dimensions,
    ISubjectRepository subjects,
    ISettingRepository settings,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<SaveDimensionRequest> saveValidator) : ISubjectDimensionService
{
    public async Task<Result<DimensionTabDto>> ListAsync(int subjectId, CancellationToken ct = default)
    {
        if (await CheckSubjectAsync(subjectId, ct) is { } denied)
        {
            return denied;
        }

        var stored = await dimensions.ListAsync(subjectId, ct);
        var types = await settings.ListActiveAsync(SettingType.DimensionType, ct);
        return new DimensionTabDto(
            types.Select(t => new LookupDto(t.Id, t.Value)).ToList(),
            stored.Select(DimensionDto.From).ToList());
    }

    public async Task<Result<DimensionDto>> CreateAsync(int subjectId, SaveDimensionRequest request, CancellationToken ct = default)
    {
        if (await CheckSubjectAsync(subjectId, ct) is { } denied)
        {
            return denied;
        }

        if (await CheckRequestAsync(subjectId, request, exceptId: null, keptTypeId: null, ct) is { } invalid)
        {
            return invalid;
        }

        var dimension = new SubjectDimension
        {
            SubjectId = subjectId,
            TypeId = request.TypeId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };
        dimensions.Add(dimension);
        await unitOfWork.SaveChangesAsync(ct);
        return DimensionDto.From((await dimensions.GetByIdAsync(dimension.Id, ct))!);
    }

    public async Task<Result<DimensionDto>> UpdateAsync(int subjectId, int dimensionId, SaveDimensionRequest request, CancellationToken ct = default)
    {
        if (await CheckSubjectAsync(subjectId, ct) is { } denied)
        {
            return denied;
        }

        var dimension = await dimensions.GetByIdAsync(dimensionId, ct);
        if (dimension is null || dimension.SubjectId != subjectId)
        {
            return Error.NotFound("Dimension", dimensionId);
        }

        if (await CheckRequestAsync(subjectId, request, exceptId: dimensionId, keptTypeId: dimension.TypeId, ct) is { } invalid)
        {
            return invalid;
        }

        // A quiz draws by one dimension type (UC-57): moving one of its groups to another
        // type would silently empty that rule, so the type stays while a rule uses it.
        if (request.TypeId != dimension.TypeId && (await dimensions.GetUseAsync(dimensionId, ct)).QuizRules > 0)
        {
            return Error.Conflict("Dimension.TypeInUse",
                $"A quiz draws its questions from \"{dimension.Name}\" as a {dimension.Type.Value}, so its type cannot change.");
        }

        dimension.TypeId = request.TypeId;
        dimension.Name = request.Name.Trim();
        dimension.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        await unitOfWork.SaveChangesAsync(ct);
        return DimensionDto.From((await dimensions.GetByIdAsync(dimensionId, ct))!);
    }

    public async Task<Result> DeleteAsync(int subjectId, int dimensionId, CancellationToken ct = default)
    {
        if (await CheckSubjectAsync(subjectId, ct) is { } denied)
        {
            return denied;
        }

        var dimension = await dimensions.GetByIdAsync(dimensionId, ct);
        if (dimension is null || dimension.SubjectId != subjectId)
        {
            return Error.NotFound("Dimension", dimensionId);
        }

        // E3: say how many records keep it.
        var use = await dimensions.GetUseAsync(dimensionId, ct);
        if (use.Any)
        {
            return Error.Conflict("Dimension.InUse",
                $"\"{dimension.Name}\" is used by {Count(use.Questions, "question")}, {Count(use.Practices, "practice")} " +
                $"and {Count(use.QuizRules, "quiz rule")}, so it cannot be deleted.");
        }

        dimensions.Remove(dimension);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>BR-11 for the subject the dimensions belong to: E1 for another Expert's subject.</summary>
    private async Task<Error?> CheckSubjectAsync(int subjectId, CancellationToken ct)
    {
        if (CourseAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var subject = await subjects.GetByIdAsync(subjectId, ct);
        if (subject is null)
        {
            return Error.NotFound("Subject", subjectId);
        }

        return CourseAccess.CheckSubject(subject, expertId);
    }

    /// <summary>E2, an active Dimension Type (the stored one is kept if switched off since), and no twin in the subject.</summary>
    private async Task<Error?> CheckRequestAsync(int subjectId, SaveDimensionRequest request, int? exceptId, int? keptTypeId, CancellationToken ct)
    {
        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var type = await settings.GetByIdAsync(request.TypeId, ct);
        if (type is null || type.Type != SettingType.DimensionType
            || (type.Status == ActiveStatus.Inactive && type.Id != keptTypeId))
        {
            return Error.Invalid(nameof(SaveDimensionRequest.TypeId), "Choose a dimension type that is in use.");
        }

        if (await dimensions.ExistsAsync(subjectId, request.TypeId, request.Name.Trim(), exceptId, ct))
        {
            return Error.Invalid(nameof(SaveDimensionRequest.Name), $"The subject already has a {type.Value} named \"{request.Name.Trim()}\".");
        }

        return null;
    }

    private static string Count(int n, string what) => n == 1 ? $"1 {what}" : $"{n} {what}s";
}
