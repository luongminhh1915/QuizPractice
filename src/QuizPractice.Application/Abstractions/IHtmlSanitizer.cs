namespace QuizPractice.Application.Abstractions;

/// <summary>
/// Cleans rich text from an editor before it is stored, so a post body cannot carry a
/// script to the public pages (stored XSS). Keeps headings, lists, links and images
/// (SRS III 5.3) and removes everything else.
/// </summary>
public interface IHtmlSanitizer
{
    /// <summary>The safe HTML; empty when nothing visible is left.</summary>
    string Sanitize(string html);
}
