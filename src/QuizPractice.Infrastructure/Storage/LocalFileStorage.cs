using QuizPractice.Application.Abstractions;

namespace QuizPractice.Infrastructure.Storage;

/// <summary>
/// Saves public uploads under {webRoot}/uploads/{folder}/ with a random name (SDS III,
/// upload rules). The type and size are checked before this is called; this class
/// only makes sure a path can never leave the uploads folder.
/// </summary>
public class LocalFileStorage(string webRootPath) : IFileStorage
{
    private const string UploadsFolder = "uploads";

    public async Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default)
    {
        if (folder.Any(c => !char.IsAsciiLetterLower(c) && c != '-'))
        {
            throw new ArgumentException($"Upload folder must be lower-case letters: '{folder}'.", nameof(folder));
        }

        var directory = Path.Combine(webRootPath, UploadsFolder, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using (var file = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew))
        {
            content.Position = 0;
            await content.CopyToAsync(file, ct);
        }

        return $"/{UploadsFolder}/{folder}/{fileName}";
    }

    public void Delete(string relativePath)
    {
        var uploads = Path.GetFullPath(Path.Combine(webRootPath, UploadsFolder)) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath.TrimStart('/')));
        // A file outside uploads/ ships with the app (the seed images under /seed/) and
        // is never removed; a replaced seed thumbnail simply stays on disk.
        if (!fullPath.StartsWith(uploads, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        File.Delete(fullPath);
    }
}
