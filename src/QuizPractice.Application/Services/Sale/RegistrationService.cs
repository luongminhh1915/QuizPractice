using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Application.Services.Public;
using QuizPractice.Application.Services.TestContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Sale;

/// <summary>
/// The Sale role sees every registration, online or recorded by any Sale user (F#35). The
/// controller requires the Registrations menu permission (D23), so only Sale reaches it.
/// </summary>
public class RegistrationService(
    IRegistrationRepository registrations,
    IQuizRepository quizzes,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<RegistrationListRequest> listValidator,
    IValidator<SaveRegistrationRequest> saveValidator,
    IValidator<SaveNotesRequest> notesValidator) : IRegistrationService
{
    public async Task<Result<Page<RegistrationSummaryDto>>> ListAsync(RegistrationListRequest request, CancellationToken ct = default)
    {
        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // Vietnam days, both ends included: To covers its whole day.
        var search = new RegistrationSearch(
            request.Search,
            request.SubjectId,
            request.From is { } from ? VietnamTime.StartUtc(from) : null,
            request.To is { } to ? VietnamTime.StartUtc(to.AddDays(1)) : null,
            request.Status,
            request.SortBy,
            request.Descending);
        var page = await registrations.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(RegistrationSummaryDto.From);
    }

    public async Task<Result<RegistrationFiltersDto>> GetFiltersAsync(CancellationToken ct = default)
    {
        var subjects = await quizzes.GetSubjectsAsync(expertId: null, ct);
        return new RegistrationFiltersDto(subjects.Select(s => new SaleSubjectDto(s.Id, s.Name)).ToList());
    }

    public async Task<Result<RegistrationChoicesDto>> GetChoicesAsync(CancellationToken ct = default)
    {
        var subjects = await registrations.ListOfferedSubjectsAsync(ct);
        return new RegistrationChoicesDto(subjects
            .Select(s => new SaleSubjectOptionDto(s.Id, s.Name, RegistrationRules.ActivePackages(s).Select(SalePackageDto.From).ToList()))
            .ToList());
    }

    public async Task<Result<RegistrationDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } me)
        {
            return SignInRequired;
        }

        var registration = await registrations.GetForSaleAsync(id, ct);
        return registration is null ? Error.NotFound("Registration", id) : ToDto(registration, me);
    }

    public async Task<Result<RegistrationDetailsDto>> CreateAsync(SaveRegistrationRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } me)
        {
            return SignInRequired;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var (package, account, refusal) = await CheckFormAsync(request, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        // Step 7: who recorded it and when; the status starts Submitted (BR-22).
        var registration = new Registration
        {
            SubjectId = request.SubjectId,
            PackageId = package!.Id,
            CreatedById = me,
            UpdatedById = me,
            RegisteredAt = DateTime.UtcNow,
            Status = RegistrationStatus.Submitted,
        };
        Fill(registration, request, package, account);
        registrations.Add(registration);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto((await registrations.GetForSaleAsync(registration.Id, ct))!, me);
    }

    public async Task<Result<RegistrationDetailsDto>> UpdateAsync(int id, SaveRegistrationRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } me)
        {
            return SignInRequired;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var registration = await registrations.GetForSaleAsync(id, ct);
        if (registration is null)
        {
            return Error.NotFound("Registration", id);
        }

        // Step 3 (BR-22): another Sale user's or an online registration, or a settled one,
        // changes only its status and notes.
        if (!CanEditAll(registration, me))
        {
            return Error.Conflict("Registration.NotEditable",
                "Only the status and the notes of this registration can change (BR-22). The registration is reloaded.");
        }

        var (package, account, refusal) = await CheckFormAsync(request, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        registration.SubjectId = request.SubjectId;
        registration.PackageId = package!.Id;
        registration.UpdatedById = me;
        Fill(registration, request, package, account);

        // E3: the status is a concurrency token; a settlement in the meantime wins.
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            return ChangedMeanwhile;
        }

        return ToDto((await registrations.GetForSaleAsync(id, ct))!, me);
    }

    public async Task<Result<RegistrationDetailsDto>> SaveNotesAsync(int id, SaveNotesRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } me)
        {
            return SignInRequired;
        }

        if (await notesValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var registration = await registrations.GetForSaleAsync(id, ct);
        if (registration is null)
        {
            return Error.NotFound("Registration", id);
        }

        registration.Notes = QuestionMapping.NullIfBlank(request.Notes);
        registration.UpdatedById = me;
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            return ChangedMeanwhile;
        }

        return ToDto((await registrations.GetForSaleAsync(id, ct))!, me);
    }

    /// <summary>
    /// The subject must be on sale and the package one of its active packages (E2). An
    /// email that already has an account links the registration to it (A4); a staff
    /// account is refused (Duck 12/10), since only a customer learns.
    /// </summary>
    private async Task<(PricePackage? Package, User? Account, Error? Refusal)> CheckFormAsync(SaveRegistrationRequest request, CancellationToken ct)
    {
        var subject = (await registrations.ListOfferedSubjectsAsync(ct)).FirstOrDefault(s => s.Id == request.SubjectId);
        if (subject is null)
        {
            return (null, null, Error.Invalid(nameof(request.SubjectId), "Choose a published subject with an active package."));
        }

        var package = RegistrationRules.ActivePackages(subject).FirstOrDefault(p => p.Id == request.PackageId);
        if (package is null)
        {
            return (null, null, RegistrationRules.PackageNotAvailable);
        }

        var account = await users.GetByEmailAsync(request.Email.Trim(), ct);
        if (account is not null && account.Role.Value != Roles.Customer)
        {
            return (null, null, Error.Invalid(nameof(request.Email), "This email belongs to a staff account. Use the customer's own email."));
        }

        return (package, account, null);
    }

    /// <summary>Steps 5 and 6: contact, notes and the total cost of the package.</summary>
    private static void Fill(Registration r, SaveRegistrationRequest request, PricePackage package, User? account)
    {
        r.UserId = account?.Id;
        r.FullName = request.FullName.Trim();
        r.Gender = request.Gender!.Value;
        r.Email = request.Email.Trim();
        r.Mobile = request.Mobile.Trim();
        r.TotalCost = package.SalePrice;
        r.Notes = QuestionMapping.NullIfBlank(request.Notes);
    }

    private static bool InUnpaidInvoice(Registration r) => r.Invoice is { Status: InvoiceStatus.Unpaid };

    private static bool CanSettle(Registration r) => r.Status == RegistrationStatus.Submitted && !InUnpaidInvoice(r);

    private static bool CanEditAll(Registration r, int me) => r.CreatedById == me && CanSettle(r);

    private static RegistrationDetailsDto ToDto(Registration r, int me) =>
        new(r.Id,
            r.SubjectId,
            r.Subject.Name,
            SalePackageDto.From(r.Package),
            r.FullName,
            r.Gender,
            r.Email,
            r.Mobile,
            r.TotalCost,
            r.Status,
            DateTime.SpecifyKind(r.RegisteredAt, DateTimeKind.Utc),
            r.CreatedBy?.Email,
            r.UpdatedBy.Email,
            r.ValidFrom,
            r.ValidTo,
            r.Notes,
            r.Invoice is { } i ? new RegistrationInvoiceDto(i.Id, i.Code, i.Status, i.TotalAmount) : null,
            r.UserId is not null,
            CanEditAll(r, me),
            CanSettle(r));

    private static Error SignInRequired => new(ErrorType.Unauthorized, "Auth.Required", "Log in as a Sale user.");

    private static Error ChangedMeanwhile =>
        Error.Conflict("Registration.Changed", "Another user has changed this registration. It is reloaded; make the change again.");
}
