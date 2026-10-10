using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.TestContent;

public class QuestionService(
    IQuestionRepository questions,
    IUnitOfWork unitOfWork,
    IPrivateFileStorage mediaFiles,
    ICurrentUser currentUser,
    IValidator<SaveQuestionRequest> saveValidator,
    IValidator<QuestionListRequest> listValidator) : IQuestionService
{
    public async Task<Result<Page<QuestionSummaryDto>>> ListAsync(QuestionListRequest request, CancellationToken ct = default)
    {
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        if (request.SubjectId is { } subjectId && await CheckSubjectAsync(subjectId, expertId, ct) is { } unreachable)
        {
            return unreachable;
        }

        var search = new QuestionSearch(expertId, request.Search, request.SubjectId, request.LessonId,
            request.DimensionIds ?? [], request.LevelId, request.Status, request.SortBy, request.Descending);
        var page = await questions.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(QuestionSummaryDto.From);
    }

    public async Task<Result<QuestionFiltersDto>> GetFiltersAsync(int? subjectId, CancellationToken ct = default)
    {
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        var subjects = await questions.GetSubjectsAsync(expertId, ct);
        var levels = await questions.GetLevelsAsync(ct);
        IReadOnlyList<QuestionLessonDto> lessons = [];
        IReadOnlyList<QuestionDimensionDto> dimensions = [];

        if (subjectId is { } id)
        {
            if (subjects.All(s => s.Id != id))
            {
                return expertId is null ? Error.NotFound("Subject", id) : NotAssigned;
            }

            lessons = (await questions.GetLessonsAsync(id, ct)).Select(QuestionLessonDto.From).ToList();
            dimensions = (await questions.GetDimensionsAsync(id, ct)).Select(QuestionDimensionDto.From).ToList();
        }

        return new QuestionFiltersDto(
            subjects.Select(s => new QuestionLookupDto(s.Id, s.Name)).ToList(),
            levels.Select(l => new QuestionLookupDto(l.Id, l.Value)).ToList(),
            lessons,
            dimensions);
    }

    public async Task<Result<QuestionDto>> GetAsync(int id, CancellationToken ct = default)
    {
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        var question = await questions.GetWithOptionsAsync(id, ct);
        if (question is null)
        {
            return Error.NotFound("Question", id);
        }

        // UC-53 E1: an Expert opens a question of a subject not assigned to that Expert.
        if (expertId is not null && question.Subject.ExpertId != expertId)
        {
            return NotAssigned;
        }

        return QuestionDto.From(question, await questions.IsUsedAsync(id, ct));
    }

    public async Task<Result<QuestionDto>> CreateAsync(SaveQuestionRequest request, CancellationToken ct = default)
    {
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        if (await CheckChoicesAsync(request, expertId, keptLevelId: null, ct) is { } badChoice)
        {
            return badChoice;
        }

        var question = QuestionMapping.NewQuestion(request, request.Media is null ? null : await SaveMediaAsync(request.Media, ct));
        questions.Add(question);
        await unitOfWork.SaveChangesAsync(ct);

        // Reloaded so the subject name and the option ids are filled in.
        return QuestionDto.From((await questions.GetWithOptionsAsync(question.Id, ct))!, isUsed: false);
    }

    public async Task<Result<QuestionDto>> UpdateAsync(int id, SaveQuestionRequest request, CancellationToken ct = default)
    {
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var question = await questions.GetWithOptionsAsync(id, ct);
        if (question is null)
        {
            return Error.NotFound("Question", id);
        }

        if (expertId is not null && question.Subject.ExpertId != expertId)
        {
            return NotAssigned;
        }

        if (request.Version != QuestionVersion.Of(question))
        {
            return Error.Conflict("Question.Changed",
                "Another user has changed this question since you opened it. The current values are loaded; make your change again.");
        }

        if (await CheckChoicesAsync(request, expertId, question.LevelId, ct) is { } badChoice)
        {
            return badChoice;
        }

        // A quiz or an attempt drawn from the old subject would end up holding a question
        // of another subject (Duck, 10/10).
        var isUsed = await questions.IsUsedAsync(id, ct);
        if (request.SubjectId != question.SubjectId && isUsed)
        {
            return Error.Conflict("Question.InUse",
                "A quiz or a customer's attempt already uses this question, so it cannot move to another subject.");
        }

        var keptIds = request.Options.Where(o => o.Id is not null).Select(o => o.Id!.Value).ToHashSet();
        var stored = question.Options.ToDictionary(o => o.Id);
        if (keptIds.Any(optionId => !stored.ContainsKey(optionId)))
        {
            return Error.Invalid(nameof(request.Options), "An option does not belong to this question. Reload the question.");
        }

        // UC-54 A2: an attempt keeps the option its customer chose, so that option stays.
        var removed = question.Options.Where(o => !keptIds.Contains(o.Id)).ToList();
        if (removed.Count > 0)
        {
            var answered = await questions.GetAnsweredOptionIdsAsync(id, ct);
            if (removed.Any(o => answered.Contains(o.Id)))
            {
                return Error.Conflict("Question.OptionAnswered",
                    "Customers have already chosen an option you removed. Keep it (you may change its text) so their attempts stay as they were answered.");
            }
        }

        string? replacedMedia = null;
        if (request.Media is not null)
        {
            replacedMedia = question.Media;
            question.Media = await SaveMediaAsync(request.Media, ct);
        }
        else if (request.RemoveMedia)
        {
            replacedMedia = question.Media;
            question.Media = null;
        }

        await unitOfWork.ExecuteInTransactionAsync(async c =>
        {
            // Step 1: no option is correct and the removed ones are gone, so the
            // one-correct-option index never sees two correct rows while step 2 runs.
            foreach (var option in question.Options)
            {
                option.IsCorrect = false;
            }

            foreach (var option in removed)
            {
                questions.RemoveOption(option);
            }

            await unitOfWork.SaveChangesAsync(c);

            // Step 2: the options in the new order, then the other fields.
            for (var i = 0; i < request.Options.Count; i++)
            {
                var input = request.Options[i];
                var option = input.Id is { } optionId ? stored[optionId] : new AnswerOption();
                option.Content = input.Content.Trim();
                option.IsCorrect = input.IsCorrect;
                option.DisplayOrder = (byte)(i + 1);
                if (input.Id is null)
                {
                    question.Options.Add(option);
                }
            }

            foreach (var link in question.Dimensions.Where(d => !request.DimensionIds.Contains(d.DimensionId)).ToList())
            {
                questions.RemoveDimension(link);
            }

            var linked = question.Dimensions.Select(d => d.DimensionId).ToHashSet();
            foreach (var dimensionId in request.DimensionIds.Where(d => !linked.Contains(d)))
            {
                question.Dimensions.Add(new QuestionDimension { DimensionId = dimensionId });
            }

            question.SubjectId = request.SubjectId;
            question.LessonId = request.LessonId;
            question.LevelId = request.LevelId;
            question.Status = request.Status;
            question.Content = request.Content.Trim();
            question.Explanation = NullIfBlank(request.Explanation);
            await unitOfWork.SaveChangesAsync(c);
        }, ct);

        // Only after the commit: if it had failed, the row would still point at the old file.
        if (replacedMedia is not null)
        {
            mediaFiles.Delete(replacedMedia);
        }

        // Reloaded so a new subject's name and the new option ids are filled in.
        return QuestionDto.From((await questions.GetWithOptionsAsync(id, ct))!, isUsed);
    }

    public async Task<Result<MediaFile>> GetMediaAsync(int id, CancellationToken ct = default)
    {
        // TODO(learning): a customer taking a practice or an exam must also reach the media
        // of the questions in that attempt (Hiếu, UC-24); only the question bank users can now.
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        var question = await questions.GetByIdAsync(id, ct);
        if (question is null)
        {
            return Error.NotFound("Question", id);
        }

        if (expertId is not null && question.Subject.ExpertId != expertId)
        {
            return NotAssigned;
        }

        var type = QuestionMedia.TypeOf(question.Media);
        var content = type is null ? null : mediaFiles.OpenRead(question.Media!);
        return content is null
            ? new Error(ErrorType.NotFound, "Question.NoMedia", $"Question {id} has no media file.")
            : new MediaFile(content, type!.ContentType);
    }

    public async Task<Result<QuestionSummaryDto>> ChangeStatusAsync(int id, ChangeQuestionStatusRequest request, CancellationToken ct = default)
    {
        if (CheckRole(out var expertId) is { } denied)
        {
            return denied;
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Error.Invalid(nameof(request.Status), "Status must be Shown or Hidden.");
        }

        var question = await questions.GetByIdAsync(id, ct);
        if (question is null)
        {
            return Error.NotFound("Question", id);
        }

        // UC-54 E1: an Expert on a question of a subject not assigned to that Expert.
        if (expertId is not null && question.Subject.ExpertId != expertId)
        {
            return NotAssigned;
        }

        // A1: already in the chosen state (perhaps set by another user meanwhile), so
        // nothing is written and the list shows the current state. A2 needs nothing here:
        // hiding deletes nothing, so a stored attempt still reads the question and the
        // option chosen (AttemptQuestion); only new draws skip a Hidden question.
        if (question.Status != request.Status)
        {
            question.Status = request.Status;
            await unitOfWork.SaveChangesAsync(ct);
        }

        return QuestionSummaryDto.From(question);
    }

    private Error? CheckRole(out int? expertId) => ContentAccess.Check(currentUser, out expertId);

    /// <summary>
    /// The subject must be one the user may reach (E1), and the lesson, the dimensions
    /// and the level must fit it. A level no longer in use stays only on the question
    /// that already has it (keptLevelId).
    /// </summary>
    private async Task<Error?> CheckChoicesAsync(SaveQuestionRequest request, int? expertId, int? keptLevelId, CancellationToken ct)
    {
        var subjects = await questions.GetSubjectsAsync(expertId, ct);
        if (subjects.All(s => s.Id != request.SubjectId))
        {
            return expertId is null ? Error.Invalid(nameof(request.SubjectId), "This subject does not exist.") : NotAssigned;
        }

        if (request.LessonId is { } lessonId
            && (await questions.GetLessonsAsync(request.SubjectId, ct)).All(l => l.Id != lessonId))
        {
            return Error.Invalid(nameof(request.LessonId), "Choose a lesson of the chosen subject.");
        }

        if (request.DimensionIds.Count > 0)
        {
            var dimensionIds = (await questions.GetDimensionsAsync(request.SubjectId, ct)).Select(d => d.Id).ToHashSet();
            if (request.DimensionIds.Any(d => !dimensionIds.Contains(d)))
            {
                return Error.Invalid(nameof(request.DimensionIds), "Choose dimensions of the chosen subject.");
            }
        }

        var level = (await questions.GetLevelsAsync(ct)).FirstOrDefault(l => l.Id == request.LevelId);
        if (level is null || (level.Status == ActiveStatus.Inactive && level.Id != keptLevelId))
        {
            return Error.Invalid(nameof(request.LevelId), "Choose a level in use.");
        }

        return null;
    }

    private Task<string> SaveMediaAsync(FileUpload media, CancellationToken ct) =>
        mediaFiles.SaveAsync(media.Content, media.Extension, QuestionMedia.Folder, ct);

    private static string? NullIfBlank(string? value) => QuestionMapping.NullIfBlank(value);

    /// <summary>UC-52 E1: an Expert asks for a subject not assigned to that Expert.</summary>
    private async Task<Error?> CheckSubjectAsync(int subjectId, int? expertId, CancellationToken ct)
    {
        if (expertId is null)
        {
            return null;
        }

        var subjects = await questions.GetSubjectsAsync(expertId, ct);
        return subjects.Any(s => s.Id == subjectId) ? null : NotAssigned;
    }

    private static readonly Error NotAssigned = ContentAccess.NotAssigned;
}
