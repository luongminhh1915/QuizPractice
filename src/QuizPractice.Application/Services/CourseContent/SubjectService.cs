using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.CourseContent;

public class SubjectService(
    ISubjectRepository subjects,
    ISettingRepository settings,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IFileStorage files,
    IHtmlSanitizer sanitizer,
    ICurrentUser currentUser,
    IValidator<CreateSubjectRequest> createValidator,
    IValidator<UpdateSubjectRequest> updateValidator,
    IValidator<SubjectListRequest> listValidator) : ISubjectService
{
    private const string ThumbnailFolder = "subjects";

    public async Task<Result<Page<SubjectSummaryDto>>> ListAsync(SubjectListRequest request, CancellationToken ct = default)
    {
        if (CourseAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // Step 2: an Expert only ever receives the assigned subjects; none yet is an empty list (A4).
        var search = new ManagedSubjectSearch(
            expertId, request.Search, request.CategoryId, request.Status, request.SortBy, request.Descending);
        var page = await subjects.SearchManagedAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        var lessons = await subjects.CountLessonsAsync(page.Items.Select(s => s.Id).ToList(), ct);
        return page.Map(s => SubjectSummaryDto.From(s, lessons.GetValueOrDefault(s.Id)));
    }

    public async Task<Result<SubjectFiltersDto>> GetFiltersAsync(CancellationToken ct = default)
    {
        if (CourseAccess.Check(currentUser, out _) is { } denied)
        {
            return denied;
        }

        var categories = await settings.ListActiveAsync(SettingType.SubjectCategory, ct);
        return new SubjectFiltersDto(categories.Select(c => new LookupDto(c.Id, c.Value)).ToList());
    }

    public async Task<Result<SubjectChoicesDto>> GetChoicesAsync(CancellationToken ct = default)
    {
        if (CourseAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var categories = await settings.ListActiveAsync(SettingType.SubjectCategory, ct);

        // Only the Admin chooses an owner (UC-42 step 2, UC-43); an Expert reads it.
        var experts = expertId is null ? await users.ListActiveByRoleAsync(Roles.Expert, ct) : [];
        return new SubjectChoicesDto(
            categories.Select(c => new LookupDto(c.Id, c.Value)).ToList(),
            experts.Select(u => new LookupDto(u.Id, $"{u.FullName} ({u.Email})")).ToList());
    }

    public async Task<Result<SubjectDto>> GetAsync(int id, CancellationToken ct = default)
    {
        if (CourseAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var subject = await subjects.GetByIdAsync(id, ct);
        if (subject is null)
        {
            return Error.NotFound("Subject", id);
        }

        // UC-44 E1: an Expert opens a subject not assigned to that Expert.
        if (CourseAccess.CheckSubject(subject, expertId) is { } notAssigned)
        {
            return notAssigned;
        }

        return SubjectDto.From(subject, await subjects.HasActivePackageAsync(id, ct));
    }

    public async Task<Result<SubjectDto>> CreateAsync(CreateSubjectRequest request, CancellationToken ct = default)
    {
        if (CourseAccess.CheckAdmin(currentUser) is { } denied)
        {
            return denied;
        }

        if (await createValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var category = await CheckCategoryAsync(request.CategoryId, keptCategoryId: null, ct);
        if (category.Error is { } badCategory)
        {
            return badCategory;
        }

        var owner = await CheckOwnerAsync(request.ExpertId, ct);
        if (owner.Error is { } badOwner)
        {
            return badOwner;
        }

        var description = sanitizer.Sanitize(request.Description ?? string.Empty);
        if (CheckDescription(description) is { } badDescription)
        {
            return badDescription;
        }

        var now = DateTime.UtcNow;
        var subject = new Subject
        {
            Name = request.Name.Trim(),
            Category = category.Value,
            Expert = owner.Value,
            IsFeatured = request.IsFeatured,
            PassRate = (byte)request.PassRate,
            Description = description,
            Status = SubjectStatus.Unpublished,
            CreatedAt = now,
            UpdatedAt = now,
            Thumbnail = await files.SaveAsync(request.Thumbnail!.Content, request.Thumbnail.Extension, ThumbnailFolder, ct),
        };
        subjects.Add(subject);
        await unitOfWork.SaveChangesAsync(ct);
        return SubjectDto.From(subject, hasActivePackage: false);
    }

    public async Task<Result<SubjectDto>> UpdateAsync(int id, UpdateSubjectRequest request, CancellationToken ct = default)
    {
        if (CourseAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var subject = await subjects.GetByIdAsync(id, ct);
        if (subject is null)
        {
            return Error.NotFound("Subject", id);
        }

        // E1: an Expert on a subject not assigned to that Expert.
        if (CourseAccess.CheckSubject(subject, expertId) is { } notAssigned)
        {
            return notAssigned;
        }

        if (await updateValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var category = await CheckCategoryAsync(request.CategoryId, keptCategoryId: subject.CategoryId, ct);
        if (category.Error is { } badCategory)
        {
            return badCategory;
        }

        // UC-43: only the Admin changes the owner; the Expert sees it read-only (UC-44 step 2).
        var ownerChanged = request.ExpertId != subject.ExpertId;
        if (ownerChanged && expertId is not null)
        {
            return CourseAccess.AdminOnly;
        }

        var owner = ownerChanged ? await CheckOwnerAsync(request.ExpertId, ct) : Result<User?>.Success(subject.Expert);
        if (owner.Error is { } badOwner)
        {
            return badOwner;
        }

        var description = sanitizer.Sanitize(request.Description ?? string.Empty);
        if (CheckDescription(description) is { } badDescription)
        {
            return badDescription;
        }

        string? replacedThumbnail = null;
        if (request.Thumbnail is not null)
        {
            replacedThumbnail = subject.Thumbnail;
            subject.Thumbnail = await files.SaveAsync(request.Thumbnail.Content, request.Thumbnail.Extension, ThumbnailFolder, ct);
        }

        // The new owner reaches the subject from now on and the previous one no longer does
        // (UC-43 post-condition): every Expert query filters on this column (BR-11).
        subject.Expert = owner.Value;
        subject.ExpertId = owner.Value?.Id;

        // Publication stays as it is (UC-44 post-condition); the price packages are not on
        // this tab at all.
        subject.Name = request.Name.Trim();
        subject.Tagline = string.IsNullOrWhiteSpace(request.Tagline) ? null : request.Tagline.Trim();
        subject.Brief = request.Brief?.Trim() ?? string.Empty;
        subject.Category = category.Value;
        subject.IsFeatured = request.IsFeatured;
        subject.PassRate = (byte)request.PassRate;
        subject.Description = description;
        subject.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

        // Only after the commit: if it had failed, the row would still point at the old file.
        if (replacedThumbnail is not null)
        {
            files.Delete(replacedThumbnail);
        }

        return SubjectDto.From(subject, await subjects.HasActivePackageAsync(id, ct));
    }

    /// <summary>An active Subject Category; on edit the stored one is kept even if switched off since (BR-16).</summary>
    private async Task<Result<Setting>> CheckCategoryAsync(int categoryId, int? keptCategoryId, CancellationToken ct)
    {
        var category = await settings.GetByIdAsync(categoryId, ct);
        if (category is null || category.Type != SettingType.SubjectCategory)
        {
            return Error.Invalid(nameof(CreateSubjectRequest.CategoryId), "Choose a subject category.");
        }

        if (category.Status == ActiveStatus.Inactive && category.Id != keptCategoryId)
        {
            return Error.Invalid(nameof(CreateSubjectRequest.CategoryId), $"The category \"{category.Value}\" is no longer in use.");
        }

        return category;
    }

    /// <summary>UC-42 E2, UC-43 E1: the owner must still be an Active user holding the Expert role.</summary>
    private async Task<Result<User?>> CheckOwnerAsync(int? expertId, CancellationToken ct)
    {
        if (expertId is not { } id)
        {
            return Result<User?>.Success(null);
        }

        var user = await users.GetByIdAsync(id, ct);
        if (user is null || user.Role.Value != Roles.Expert || user.Status != UserStatus.Active)
        {
            return Error.Invalid(nameof(CreateSubjectRequest.ExpertId),
                "This user no longer holds the Expert role. Choose the owner again.");
        }

        return user;
    }

    /// <summary>The limit of BR-17 applies to what is stored, after cleaning.</summary>
    private static Error? CheckDescription(string description) =>
        description.Length > FieldLength.LongText
            ? Error.Invalid(nameof(CreateSubjectRequest.Description), $"The description must be {FieldLength.LongText:N0} characters or fewer.")
            : null;
}
