using System.Globalization;
using System.IO.Compression;
using ClosedXML.Excel;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;

namespace QuizPractice.Infrastructure.Spreadsheets;

/// <summary>
/// The question import spreadsheet with ClosedXML (MIT licence; SDS II.4 names it
/// ExcelQuestionReader). Reads the first sheet only and takes cell values, never
/// formulas: a formula cell gives the value Excel saved with it.
/// </summary>
public class ExcelQuestionSheet : IQuestionSheet
{
    private const string NotXlsx = "The file is not an Excel workbook (.xlsx). Download the sample template and fill it in.";

    public QuestionSheetContent Read(Stream content)
    {
        if (CheckPackage(content) is { } problem)
        {
            return QuestionSheetContent.Refused(problem);
        }

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(content);
        }
        catch (Exception e) when (e is InvalidDataException or IOException or FormatException or ArgumentException or InvalidOperationException)
        {
            return QuestionSheetContent.Refused(NotXlsx);
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.First();
            var columns = QuestionImport.Columns;
            var headings = Enumerable.Range(1, columns.Count).Select(c => Text(sheet.Cell(1, c)) ?? "").ToList();
            if (!headings.SequenceEqual(columns, StringComparer.OrdinalIgnoreCase))
            {
                return QuestionSheetContent.Refused(
                    $"The first row must hold the template headings, in this order: {string.Join(", ", columns)}.");
            }

            var rows = new List<QuestionSheetRow>();
            var lastLine = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var line = 2; line <= lastLine; line++)
            {
                var cells = Enumerable.Range(1, columns.Count).Select(c => Text(sheet.Cell(line, c))).ToList();
                if (cells.All(c => c is null))
                {
                    continue;
                }

                if (rows.Count == QuestionImport.MaxRows)
                {
                    return QuestionSheetContent.Refused(
                        $"The file has more than {QuestionImport.MaxRows} questions. Split it into several files.");
                }

                rows.Add(new QuestionSheetRow(
                    line, cells[0], cells[1], cells[2], cells[3],
                    cells.GetRange(4, QuestionImport.OptionColumns),
                    cells[4 + QuestionImport.OptionColumns], cells[5 + QuestionImport.OptionColumns]));
            }

            return rows.Count == 0
                ? QuestionSheetContent.Refused("The file has the headings but no question rows.")
                : new QuestionSheetContent(rows, null);
        }
    }

    public byte[] WriteTemplate(QuestionTemplateChoices choices)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: the headings only, so no example row is imported by mistake.
        var questions = workbook.AddWorksheet("Questions");
        for (var c = 0; c < QuestionImport.Columns.Count; c++)
        {
            questions.Cell(1, c + 1).Value = QuestionImport.Columns[c];
            questions.Column(c + 1).Width = c == 3 ? 60 : c is >= 4 and <= 9 ? 28 : 18;
        }

        questions.Row(1).Style.Font.Bold = true;
        questions.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E6F4FF");
        questions.SheetView.FreezeRows(1);

        // Sheet 2: the exact names this subject accepts, and how to fill a row.
        var help = workbook.AddWorksheet("Choices");
        var row = 1;
        void Line(params string[] values)
        {
            for (var c = 0; c < values.Length; c++)
            {
                help.Cell(row, c + 1).Value = values[c];
            }

            row++;
        }

        void Heading(string text)
        {
            row++;
            help.Cell(row, 1).Value = text;
            help.Cell(row, 1).Style.Font.Bold = true;
            row++;
        }

        Line($"Question import template for: {choices.Subject}");
        help.Cell(1, 1).Style.Font.Bold = true;
        Line("Fill one question per row on the Questions sheet. Names must match the lists below (capital letters do not matter).");
        Line($"Dimensions: one or more names separated by '{QuestionImport.DimensionSeparator}'. Correct: the number of the right option (1 to 6).");
        Line("Lesson, Dimensions and Explanation may stay empty. Imported questions arrive Hidden: check each one, then Show it.");

        Heading("Example row (copy the idea, not the row)");
        Line([.. QuestionImport.Columns]);
        Line(choices.Lessons.FirstOrDefault().Lesson ?? "", string.Join("; ", choices.Dimensions.Take(2).Select(d => d.Name)),
            choices.Levels.FirstOrDefault() ?? "", "Which keyword creates an object?", "new", "make", "create", "", "", "", "1",
            "new allocates the object and calls a constructor.");

        Heading("Lessons (Topic, Lesson)");
        foreach (var (topic, lesson) in choices.Lessons)
        {
            Line(topic, lesson);
        }

        Heading("Dimensions (Type, Name)");
        foreach (var (type, name) in choices.Dimensions)
        {
            Line(type, name);
        }

        Heading("Levels");
        foreach (var level in choices.Levels)
        {
            Line(level);
        }

        help.Column(1).Width = 45;
        help.Column(2).Width = 30;

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    /// <summary>E2: the file must be a zip package that does not unpack into more than the limit.</summary>
    private static string? CheckPackage(Stream content)
    {
        try
        {
            content.Position = 0;
            using var zip = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            long unpacked = 0;
            foreach (var entry in zip.Entries)
            {
                unpacked += entry.Length;
                if (unpacked > QuestionImport.MaxUnpackedBytes)
                {
                    return "The file unpacks into more than 50 MB, far more than 500 questions need. Use the sample template.";
                }
            }

            return null;
        }
        catch (InvalidDataException)
        {
            return NotXlsx;
        }
        finally
        {
            content.Position = 0;
        }
    }

    private static string? Text(IXLCell cell)
    {
        var value = cell.HasFormula ? cell.CachedValue : cell.Value;
        var text = value.Type switch
        {
            XLDataType.Blank => null,
            XLDataType.Text => value.GetText(),
            XLDataType.Number => value.GetNumber().ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => value.GetBoolean() ? "TRUE" : "FALSE",
            XLDataType.DateTime => value.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            XLDataType.TimeSpan => value.GetTimeSpan().ToString(),
            _ => value.ToString(),
        };
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
