using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Validators.TestContent;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>
/// UC-55 (SDS II.4). Every row goes through the same rules as Question Details (UC-53):
/// the names in the file become the ids of the subject's lessons, dimensions and levels,
/// then SaveQuestionRequestValidator checks the row. The valid rows are stored Hidden in
/// one SaveChanges, so a database failure stores none of them (E4). Duplicates are not
/// checked here: the Hidden review catches them (SDS II.4).
/// </summary>
public partial class QuestionImportService(
    IQuestionRepository questions,
    IUnitOfWork unitOfWork,
    IQuestionSheet sheet,
    ICurrentUser currentUser,
    IValidator<SaveQuestionRequest> saveValidator) : IQuestionImportService
{
    private const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Result<QuestionImportResultDto>> ImportAsync(int subjectId, FileUpload? file, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var (subject, unreachable) = await FindSubjectAsync(subjectId, expertId, ct);
        if (unreachable is not null)
        {
            return unreachable;
        }

        if (CheckFile(file) is { } badFile)
        {
            return badFile;
        }

        // E2: a file that does not follow the template is refused as a whole.
        var content = sheet.Read(file!.Content);
        if (content.Problem is not null)
        {
            return Error.Invalid("File", content.Problem);
        }

        var choices = await Choices.LoadAsync(questions, subject!, ct);
        var rejected = new List<RejectedRowDto>();
        var imported = 0;
        foreach (var row in content.Rows)
        {
            var (request, reasons) = await CheckRowAsync(row, subject!, choices, ct);
            if (reasons.Count > 0)
            {
                rejected.Add(new RejectedRowDto(row.Line, reasons));
                continue;
            }

            questions.Add(QuestionMapping.NewQuestion(request, media: null));
            imported++;
        }

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        return new QuestionImportResultDto(subject!.Id, subject.Name, content.Rows.Count, imported, rejected);
    }

    public async Task<Result<FileDownload>> GetTemplateAsync(int subjectId, CancellationToken ct = default)
    {
        if (ContentAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var (subject, unreachable) = await FindSubjectAsync(subjectId, expertId, ct);
        if (unreachable is not null)
        {
            return unreachable;
        }

        var choices = await Choices.LoadAsync(questions, subject!, ct);
        var template = sheet.WriteTemplate(new QuestionTemplateChoices(
            subject!.Name,
            choices.Lessons.Select(l => (l.Topic!.Name, l.Name)).ToList(),
            choices.Dimensions.Select(d => (d.Type.Value, d.Name)).ToList(),
            choices.Levels.Where(l => l.Status == ActiveStatus.Active).Select(l => l.Value).ToList()));

        var slug = NotLetterOrDigit().Replace(subject.Name.ToLowerInvariant(), "-").Trim('-');
        return new FileDownload(template, XlsxType, $"question-import-{slug}.xlsx");
    }

    /// <summary>The subject must exist (Admin) or be assigned to the Expert (E1).</summary>
    private async Task<(Subject? Subject, Error? Error)> FindSubjectAsync(int subjectId, int? expertId, CancellationToken ct)
    {
        var subject = (await questions.GetSubjectsAsync(expertId, ct)).FirstOrDefault(s => s.Id == subjectId);
        if (subject is not null)
        {
            return (subject, null);
        }

        return (null, expertId is null
            ? Error.Invalid("SubjectId", "Choose the subject the questions go into.")
            : ContentAccess.NotAssigned);
    }

    /// <summary>E2 before reading: an .xlsx of at most 5 MB (SDS II.4). The real format is checked by reading it.</summary>
    private static Error? CheckFile(FileUpload? file)
    {
        if (file is null || file.Length == 0)
        {
            return Error.Invalid("File", "Choose the file to import.");
        }

        if (file.Extension != QuestionImport.Extension)
        {
            return Error.Invalid("File", "The file must be an Excel workbook (.xlsx) made from the sample template.");
        }

        return file.Length > QuestionImport.MaxFileBytes
            ? Error.Invalid("File", $"The file must be {QuestionImport.MaxFileBytes / (1024 * 1024)} MB or smaller.")
            : null;
    }

    /// <summary>Turns the names of a row into ids, then checks it as UC-53 checks the form.</summary>
    private async Task<(SaveQuestionRequest Request, List<string> Reasons)> CheckRowAsync(
        QuestionSheetRow row, Subject subject, Choices choices, CancellationToken ct)
    {
        var reasons = new List<string>();

        int? lessonId = null;
        if (row.Lesson is { } lessonName)
        {
            var matches = choices.Lessons.Where(l => Same(l.Name, lessonName)).ToList();
            if (matches.Count == 1)
            {
                lessonId = matches[0].Id;
            }
            else
            {
                reasons.Add(matches.Count == 0
                    ? $"Lesson \"{lessonName}\" is not a lesson of {subject.Name}."
                    : $"Lesson \"{lessonName}\" matches {matches.Count} lessons of {subject.Name}; leave Lesson empty or give the lessons different names.");
            }
        }

        var dimensionIds = new List<int>();
        var dimensionNames = (row.Dimensions ?? "")
            .Split(QuestionImport.DimensionSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var name in dimensionNames)
        {
            var matches = choices.Dimensions.Where(d => Same(d.Name, name)).ToList();
            if (matches.Count == 1)
            {
                dimensionIds.Add(matches[0].Id);
            }
            else
            {
                reasons.Add(matches.Count == 0
                    ? $"Dimension \"{name}\" is not a dimension of {subject.Name}."
                    : $"Dimension \"{name}\" matches {matches.Count} dimensions of {subject.Name}.");
            }
        }

        var levelId = 0;
        var levelReported = true;
        var level = row.Level is null ? null : choices.Levels.FirstOrDefault(l => Same(l.Value, row.Level));
        var levelNames = string.Join(", ", choices.Levels.Where(l => l.Status == ActiveStatus.Active).Select(l => l.Value));
        if (row.Level is null)
        {
            reasons.Add($"Level is empty; write one of: {levelNames}.");
        }
        else if (level is null)
        {
            reasons.Add($"Level \"{row.Level}\" is not a question level; write one of: {levelNames}.");
        }
        else if (level.Status == ActiveStatus.Inactive)
        {
            reasons.Add($"Level \"{row.Level}\" is no longer in use; write one of: {levelNames}.");
        }
        else
        {
            levelId = level.Id;
            levelReported = false;
        }

        // The options run from Option 1 to the last filled cell, without a gap.
        var last = row.Options.ToList().FindLastIndex(o => o is not null);
        var options = row.Options.Take(last + 1).ToList();
        var gap = options.FindIndex(o => o is null);
        if (gap >= 0)
        {
            reasons.Add($"Option {gap + 1} is empty but a later option is filled.");
        }

        var correct = 0;
        var correctReported = true;
        if (row.Correct is null)
        {
            reasons.Add("Correct is empty; write the number of the right option.");
        }
        else if (!int.TryParse(row.Correct, NumberStyles.Integer, CultureInfo.InvariantCulture, out correct)
                 || correct < 1 || correct > Math.Max(options.Count, 1))
        {
            reasons.Add($"Correct must be the number of a filled option, from 1 to {Math.Max(options.Count, 1)}.");
        }
        else
        {
            correctReported = false;
        }

        var request = new SaveQuestionRequest(
            subject.Id, lessonId, levelId, VisibilityStatus.Hidden, dimensionIds,
            row.Content ?? string.Empty, row.Explanation,
            options.Select((o, i) => new AnswerOptionInput(null, o ?? string.Empty, i + 1 == correct)).ToList());

        // The form rules of UC-53, without repeating what the names above already reported.
        var result = await saveValidator.ValidateAsync(request, ct);
        reasons.AddRange(result.Errors
            .Where(e => !(levelReported && e.PropertyName == nameof(SaveQuestionRequest.LevelId)))
            .Where(e => !(correctReported && e.ErrorCode == SaveQuestionRequestValidator.OneCorrect))
            .Where(e => !(gap >= 0 && e.PropertyName.StartsWith($"{nameof(SaveQuestionRequest.Options)}[")))
            .Select(e => e.ErrorMessage)
            .Distinct());

        return (request, reasons);
    }

    private static bool Same(string stored, string written) =>
        string.Equals(stored.Trim(), written.Trim(), StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotLetterOrDigit();

    /// <summary>The names a row of this subject may use.</summary>
    private sealed record Choices(IReadOnlyList<Lesson> Lessons, IReadOnlyList<SubjectDimension> Dimensions, IReadOnlyList<Setting> Levels)
    {
        public static async Task<Choices> LoadAsync(IQuestionRepository questions, Subject subject, CancellationToken ct) =>
            new(await questions.GetLessonsAsync(subject.Id, ct),
                await questions.GetDimensionsAsync(subject.Id, ct),
                await questions.GetLevelsAsync(ct));
    }
}
