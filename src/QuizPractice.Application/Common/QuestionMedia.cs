using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Common;

/// <summary>
/// The files a question may carry (UC-53 step 4, E3; Duck 10/10): an image, an MP3 or an
/// MP4. Kept in App_Data/question-media and streamed through the API, never from wwwroot.
/// </summary>
public static class QuestionMedia
{
    public const string Folder = "question-media";

    public sealed record MediaType(string Kind, string ContentType, long MaxBytes);

    public static readonly IReadOnlyDictionary<string, MediaType> Types = new Dictionary<string, MediaType>
    {
        [".jpg"] = new("image", "image/jpeg", FieldLength.ImageMaxBytes),
        [".jpeg"] = new("image", "image/jpeg", FieldLength.ImageMaxBytes),
        [".png"] = new("image", "image/png", FieldLength.ImageMaxBytes),
        [".webp"] = new("image", "image/webp", FieldLength.ImageMaxBytes),
        [".mp3"] = new("audio", "audio/mpeg", FieldLength.AudioMaxBytes),
        [".mp4"] = new("video", "video/mp4", FieldLength.VideoMaxBytes),
    };

    /// <summary>The type of a stored media path, or null for an unknown extension.</summary>
    public static MediaType? TypeOf(string? path) =>
        path is not null && Types.TryGetValue(Path.GetExtension(path).ToLowerInvariant(), out var type) ? type : null;
}

/// <summary>A stored file opened for streaming to the client.</summary>
public sealed record MediaFile(Stream Content, string ContentType);
