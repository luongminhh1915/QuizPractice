using Ganss.Xss;
using IHtmlSanitizer = QuizPractice.Application.Abstractions.IHtmlSanitizer;

namespace QuizPractice.Infrastructure.Security;

/// <summary>
/// Allow-list sanitizer over HtmlSanitizer (Ganss.Xss): the tags the post editor
/// produces, links to http, https and mailto only, and images only from our own
/// upload folder, so a post cannot pull a picture from another site.
/// </summary>
public class HtmlContentSanitizer : IHtmlSanitizer
{
    /// <summary>Where the editor uploads the images of a post body.</summary>
    public const string ImagePathPrefix = "/uploads/post-content/";

    private static readonly HashSet<string> DropWithContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "SCRIPT", "STYLE", "IFRAME", "OBJECT", "EMBED", "TEMPLATE", "NOSCRIPT", "SVG", "MATH", "TEXTAREA", "SELECT",
    };

    private readonly HtmlSanitizer _sanitizer;

    public HtmlContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "h2", "h3", "h4", "strong", "b", "em", "i", "u", "s",
                "ul", "ol", "li", "blockquote", "pre", "code", "hr", "a", "img",
            },
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "src", "alt", "title" },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto" },
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "src" },
            AllowedCssProperties = new HashSet<string>(),
        })
        {
            // A refused formatting tag (an <h1> pasted in) loses the tag, not the words.
            KeepChildNodes = true,
        };

        // ...but a tag that holds code or another document goes with everything in it.
        _sanitizer.RemovingTag += (_, e) =>
        {
            if (DropWithContent.Contains(e.Tag.NodeName))
            {
                e.Tag.InnerHtml = string.Empty;
            }
        };

        _sanitizer.FilterUrl += (_, e) =>
        {
            if (e.Tag.NodeName == "IMG" && !e.OriginalUrl.StartsWith(ImagePathPrefix, StringComparison.Ordinal))
            {
                e.SanitizedUrl = null;
            }
        };

        // An image whose address was refused is dropped entirely.
        _sanitizer.PostProcessDom += (_, e) =>
        {
            foreach (var img in e.Document.QuerySelectorAll("img:not([src])").ToList())
            {
                img.Remove();
            }
        };
    }

    public string Sanitize(string html)
    {
        var clean = _sanitizer.Sanitize(html).Trim();

        // "<p></p>" is what the editor sends when the user typed nothing.
        var visible = clean.Contains("<img", StringComparison.OrdinalIgnoreCase)
            || System.Text.RegularExpressions.Regex.Replace(clean, "<[^>]*>|&nbsp;", string.Empty).Trim().Length > 0;
        return visible ? clean : string.Empty;
    }
}
