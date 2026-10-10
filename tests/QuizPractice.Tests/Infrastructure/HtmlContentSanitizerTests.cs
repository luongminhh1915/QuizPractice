using QuizPractice.Infrastructure.Security;

namespace QuizPractice.Tests.Infrastructure;

/// <summary>The post body reaches the public Blogs pages, so every way to run a script must be gone.</summary>
public class HtmlContentSanitizerTests
{
    private readonly HtmlContentSanitizer _sanitizer = new();

    [Theory]
    [InlineData("<h2>Title</h2>")]
    [InlineData("<ul><li>One</li><li>Two</li></ul>")]
    [InlineData("<ol><li><strong>Bold</strong> and <em>italic</em></li></ol>")]
    [InlineData("<p><a href=\"https://example.com/page\">link</a></p>")]
    [InlineData("<p><img src=\"/uploads/post-content/7f3a.png\" alt=\"chart\"></p>")]
    [InlineData("<blockquote><p>Quote</p></blockquote>")]
    public void Keeps_what_the_editor_produces(string html)
    {
        Assert.Equal(html, _sanitizer.Sanitize(html));
    }

    [Theory]
    [InlineData("<p>a</p><script>alert(1)</script>", "<p>a</p>")]
    [InlineData("<p onclick=\"alert(1)\">a</p>", "<p>a</p>")]
    [InlineData("<p style=\"background:url(javascript:alert(1))\">a</p>", "<p>a</p>")]
    [InlineData("<p><a href=\"javascript:alert(1)\">a</a></p>", "<p><a>a</a></p>")]
    [InlineData("<p><a href=\"data:text/html,<script>alert(1)</script>\">a</a></p>", "<p><a>a</a></p>")]
    [InlineData("<p>a<iframe src=\"https://evil.example\"></iframe></p>", "<p>a</p>")]
    [InlineData("<p>a<img src=\"x\" onerror=\"alert(1)\"></p>", "<p>a</p>")]
    [InlineData("<p>a<img src=\"https://other.example/track.png\"></p>", "<p>a</p>")]
    [InlineData("<p>a</p><style>p{color:red}</style>", "<p>a</p>")]
    [InlineData("<p>a<svg onload=\"alert(1)\"><script>alert(2)</script></svg></p>", "<p>a</p>")]
    [InlineData("<h1>Big</h1>", "Big")]
    public void Removes_scripts_handlers_and_foreign_images(string dirty, string clean)
    {
        Assert.Equal(clean, _sanitizer.Sanitize(dirty));
    }

    [Theory]
    [InlineData("<p></p>")]
    [InlineData("<p>&nbsp;</p>")]
    [InlineData("<script>alert(1)</script>")]
    public void Nothing_visible_is_stored_as_empty(string html)
    {
        Assert.Equal(string.Empty, _sanitizer.Sanitize(html));
    }
}
