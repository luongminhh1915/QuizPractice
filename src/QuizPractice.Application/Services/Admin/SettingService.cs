using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Admin;

public class SettingService(
    ISettingRepository settings,
    IQuizRepository quizzes,
    IUnitOfWork unitOfWork,
    IPermissionTable permissions,
    ICurrentUser currentUser,
    IValidator<SettingListRequest> listValidator,
    IValidator<CreateSettingRequest> createValidator,
    IValidator<UpdateSettingRequest> updateValidator) : ISettingService
{
    /// <summary>
    /// Types the code reads by their values (Roles, SettingValues): a new or renamed value
    /// would break sign-in, the menu, lessons or quizzes, so they stay as seeded (Duck, 12/10).
    /// </summary>
    private static readonly SettingType[] LockedTypes =
        [SettingType.UserRole, SettingType.SystemMenu, SettingType.LessonType, SettingType.TestType];

    public async Task<Result<Page<SettingSummaryDto>>> ListAsync(SettingListRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var search = new SettingSearch(request.Search, request.Type, request.Status, request.SortBy, request.Descending);
        var page = await settings.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(s => SettingSummaryDto.From(s, CanDeactivate(s)));
    }

    public async Task<Result<SettingDto>> GetAsync(int id, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        var setting = await settings.GetByIdAsync(id, ct);
        return setting is null ? Error.NotFound("Setting", id) : ToDto(setting);
    }

    public async Task<Result<SettingSavedDto>> CreateAsync(CreateSettingRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (await createValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        if (LockedTypes.Contains(request.Type))
        {
            return Error.Invalid(nameof(request.Type), "The values of this type are read by the system and cannot be added.");
        }

        var value = request.Value.Trim();
        if (await settings.ExistsAsync(request.Type, value, exceptId: null, ct))
        {
            return Taken(value);
        }

        var setting = new Setting
        {
            Type = request.Type,
            Value = value,
            DisplayOrder = request.DisplayOrder ?? await settings.NextOrderAsync(request.Type, ct),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = request.Status,
        };
        settings.Add(setting);
        await unitOfWork.SaveChangesAsync(ct);
        return new SettingSavedDto(ToDto(setting), 0);
    }

    public async Task<Result<SettingSavedDto>> UpdateAsync(int id, UpdateSettingRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (await updateValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var setting = await settings.GetByIdAsync(id, ct);
        if (setting is null)
        {
            return Error.NotFound("Setting", id);
        }

        // E2: another Admin saved since the form was loaded.
        if (request.Version != SettingVersion.Of(setting))
        {
            return Changed;
        }

        var value = request.Value.Trim();
        var renamed = value != setting.Value;
        if (renamed && LockedTypes.Contains(setting.Type))
        {
            return Error.Invalid(nameof(request.Value), "The system reads this value, so it cannot change. Order, description and status can.");
        }

        if (renamed && await settings.ExistsAsync(setting.Type, value, exceptId: id, ct))
        {
            return Taken(value);
        }

        if (await CheckStatusAsync(setting, request.Status, request.ConfirmInUse, ct) is { } badStatus)
        {
            return badStatus;
        }

        // A quiz names the dimension type it draws by; it follows the new name in the same save (Duck, 12/10).
        var followed = 0;
        if (renamed && setting.Type == SettingType.DimensionType)
        {
            foreach (var quiz in await quizzes.ListDrawnByAsync(setting.Value, ct))
            {
                quiz.SelectionType = value;
                followed++;
            }
        }

        setting.Value = value;
        setting.DisplayOrder = request.DisplayOrder ?? setting.DisplayOrder;
        setting.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        setting.Status = request.Status;
        await unitOfWork.SaveChangesAsync(ct);
        return new SettingSavedDto(ToDto(setting), followed);
    }

    public async Task<Result<SettingSummaryDto>> ChangeStatusAsync(int id, ChangeSettingStatusRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Error.Invalid(nameof(request.Status), "Status must be Active or Inactive.");
        }

        var setting = await settings.GetByIdAsync(id, ct);
        if (setting is null)
        {
            return Error.NotFound("Setting", id);
        }

        // A1: already in the chosen state, so nothing is written.
        if (setting.Status == request.Status)
        {
            return SettingSummaryDto.From(setting, CanDeactivate(setting));
        }

        // E2: the row was loaded before another Admin changed the setting.
        if (request.Version != SettingVersion.Of(setting))
        {
            return Changed;
        }

        if (await CheckStatusAsync(setting, request.Status, request.ConfirmInUse, ct) is { } badStatus)
        {
            return badStatus;
        }

        // The screens that read the type offer only the active values from now on (step 4).
        setting.Status = request.Status;
        await unitOfWork.SaveChangesAsync(ct);
        return SettingSummaryDto.From(setting, CanDeactivate(setting));
    }

    /// <summary>
    /// Switching a value off: never a role, a lesson type, a test type or the Settings menu
    /// item (the Admin would lock itself out); and only after confirmation while records use it (UC-66 E1).
    /// </summary>
    private async Task<Error?> CheckStatusAsync(Setting setting, ActiveStatus status, bool confirmInUse, CancellationToken ct)
    {
        if (status == setting.Status || status == ActiveStatus.Active)
        {
            return null;
        }

        if (!CanDeactivate(setting))
        {
            return Error.Conflict("Setting.Required", $"\"{setting.Value}\" is needed by the system and stays Active.");
        }

        var uses = await settings.GetUseAsync(setting, ct);
        if (uses.Count > 0 && !confirmInUse)
        {
            return Error.Conflict("Setting.InUse",
                $"\"{setting.Value}\" is used by {string.Join(" and ", uses.Select(u => $"{u.Count} {u.What}"))}. " +
                "They keep it, but the screens will no longer offer it. Switch it off anyway?");
        }

        return null;
    }

    private static bool CanDeactivate(Setting s) =>
        s.Type is not (SettingType.UserRole or SettingType.LessonType or SettingType.TestType)
        && !(s.Type == SettingType.SystemMenu && s.Value == SettingValues.MenuItems.Settings);

    private SettingDto ToDto(Setting s) =>
        new(s.Id, s.Type, s.Value, s.DisplayOrder, s.Description, s.Status, SettingVersion.Of(s),
            ValueLocked: LockedTypes.Contains(s.Type),
            CanDeactivate: CanDeactivate(s),
            MenuRoles: s.Type == SettingType.SystemMenu ? Roles.All.Where(r => permissions.Allows(r, s.Value)).ToList() : []);

    private static Error Taken(string value) =>
        Error.Invalid(nameof(UpdateSettingRequest.Value), $"\"{value}\" is already a value of this type.");

    private static readonly Error Changed =
        Error.Conflict("Setting.Changed", "Another Admin changed this setting meanwhile. The current values are shown; make your change again.");
}
