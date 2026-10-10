using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Entities;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>Builds a new question from a checked form, the same way for Question Details (UC-53) and the import (UC-55).</summary>
internal static class QuestionMapping
{
    public static Question NewQuestion(SaveQuestionRequest request, string? media) => new()
    {
        SubjectId = request.SubjectId,
        LessonId = request.LessonId,
        LevelId = request.LevelId,
        Status = request.Status,
        Content = request.Content.Trim(),
        Explanation = NullIfBlank(request.Explanation),
        Media = media,
        Dimensions = request.DimensionIds.Select(d => new QuestionDimension { DimensionId = d }).ToList(),
        Options = request.Options
            .Select((o, i) => new AnswerOption { Content = o.Content.Trim(), IsCorrect = o.IsCorrect, DisplayOrder = (byte)(i + 1) })
            .ToList(),
    };

    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
