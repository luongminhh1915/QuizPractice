using FluentValidation;
using QuizPractice.Application.Common;

namespace QuizPractice.Application.Validators.TestContent;

/// <summary>
/// UC-53 E3: a JPG, PNG or WebP image up to 2 MB, an MP3 up to 10 MB or an MP4 up to
/// 20 MB. The extension alone proves nothing, so the first bytes must match the type too.
/// Internal on purpose: AddValidatorsFromAssembly skips internal types, so this never
/// replaces ImageUploadValidator as the IValidator&lt;FileUpload&gt; that PostService gets.
/// SaveQuestionRequestValidator runs it as a child rule.
/// </summary>
internal sealed class QuestionMediaValidator : AbstractValidator<FileUpload>
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();
    private static readonly byte[] Id3 = "ID3"u8.ToArray();
    private static readonly byte[] Ftyp = "ftyp"u8.ToArray();

    public QuestionMediaValidator()
    {
        RuleFor(x => x.Extension)
            .Must(e => QuestionMedia.Types.ContainsKey(e))
            .WithMessage("The media must be a JPG, PNG or WebP image, an MP3 or an MP4 file.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("The media file is empty.")
            .Must((upload, length) => length <= QuestionMedia.Types[upload.Extension].MaxBytes)
            .WithMessage(upload => $"The {QuestionMedia.Types[upload.Extension].Kind} must be {QuestionMedia.Types[upload.Extension].MaxBytes / (1024 * 1024)} MB or smaller.")
            .When(x => QuestionMedia.Types.ContainsKey(x.Extension));

        RuleFor(x => x.Content)
            .Must((upload, content) => HasSignature(content, upload.Extension))
            .WithMessage(upload => $"The file is not a real {upload.Extension.TrimStart('.').ToUpperInvariant()} file.")
            .When(x => QuestionMedia.Types.ContainsKey(x.Extension));
    }

    private static bool HasSignature(Stream content, string extension)
    {
        Span<byte> head = stackalloc byte[12];
        content.Position = 0;
        var read = content.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);
        content.Position = 0;
        var h = head[..read];
        return extension switch
        {
            ".png" => h.StartsWith(Png),
            ".jpg" or ".jpeg" => h.StartsWith(Jpg),
            ".webp" => h.Length >= 12 && h.StartsWith(Riff) && h[8..12].SequenceEqual(Webp),
            // An MP3 starts with an ID3 tag or straight with an MPEG frame (11 bits set).
            ".mp3" => h.StartsWith(Id3) || (h.Length >= 2 && h[0] == 0xFF && (h[1] & 0xE0) == 0xE0),
            ".mp4" => h.Length >= 8 && h[4..8].SequenceEqual(Ftyp),
            _ => false,
        };
    }
}
