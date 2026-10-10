namespace QuizPractice.Application.Abstractions;

/// <summary>
/// A data row of an import file: its line number in the sheet (the headings are line 1)
/// and the text of each template column, trimmed, empty cells as null.
/// </summary>
public sealed record QuestionSheetRow(
    int Line,
    string? Lesson,
    string? Dimensions,
    string? Level,
    string? Content,
    IReadOnlyList<string?> Options,
    string? Correct,
    string? Explanation);

/// <summary>The rows of a file, or why the whole file is refused (UC-55 E2).</summary>
public sealed record QuestionSheetContent(IReadOnlyList<QuestionSheetRow> Rows, string? Problem)
{
    public static QuestionSheetContent Refused(string problem) => new([], problem);
}

/// <summary>What the template lists for one subject, so the person filling it copies exact names.</summary>
public sealed record QuestionTemplateChoices(
    string Subject,
    IReadOnlyList<(string Topic, string Lesson)> Lessons,
    IReadOnlyList<(string Type, string Name)> Dimensions,
    IReadOnlyList<string> Levels);

/// <summary>
/// Reads and writes the question import spreadsheet (UC-55; template in QuestionImport).
/// Infrastructure implements it with ClosedXML, so Application never sees a file format.
/// </summary>
public interface IQuestionSheet
{
    /// <summary>Reads the first sheet: cell values only, never formulas. Blank rows are skipped.</summary>
    QuestionSheetContent Read(Stream content);

    /// <summary>An empty template for a subject: the headings, and a second sheet with its choices and an example.</summary>
    byte[] WriteTemplate(QuestionTemplateChoices choices);
}
