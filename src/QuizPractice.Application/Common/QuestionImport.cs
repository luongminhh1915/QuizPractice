namespace QuizPractice.Application.Common;

/// <summary>
/// The import template of UC-55 (SRS III 7.3) and its limits (SDS II.4; Duck 10/10).
/// One question per row; the subject is chosen in the pop-up, not in the file, and media
/// is attached afterwards in Question Details.
/// </summary>
public static class QuestionImport
{
    public const int OptionColumns = 6;

    /// <summary>The headings of the first row, in order.</summary>
    public static readonly IReadOnlyList<string> Columns =
    [
        "Lesson", "Dimensions", "Level", "Content",
        "Option 1", "Option 2", "Option 3", "Option 4", "Option 5", "Option 6",
        "Correct", "Explanation",
    ];

    public const string Extension = ".xlsx";
    public const long MaxFileBytes = 5 * 1024 * 1024;

    /// <summary>An .xlsx is a zip: a small file can unpack into a huge one (zip bomb).</summary>
    public const long MaxUnpackedBytes = 50 * 1024 * 1024;

    public const int MaxRows = 500;

    /// <summary>Separates several dimensions in one cell.</summary>
    public const char DimensionSeparator = ';';
}
