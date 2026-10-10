using QuizPractice.Application.Abstractions;

namespace QuizPractice.Infrastructure.Storage;

/// <summary>
/// Saves private uploads under {rootPath}/{folder}/ (App_Data, outside wwwroot, so the
/// static file middleware never serves them). The type and size are checked before this
/// is called; this class only makes sure a path can never leave the root.
/// </summary>
public class PrivateFileStorage(string rootPath) : IPrivateFileStorage
{
    public async Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default)
    {
        if (folder.Any(c => !char.IsAsciiLetterLower(c) && c != '-'))
        {
            throw new ArgumentException($"Folder must be lower-case letters: '{folder}'.", nameof(folder));
        }

        var directory = Path.Combine(rootPath, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using (var file = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew))
        {
            content.Position = 0;
            await content.CopyToAsync(file, ct);
        }

        return $"{folder}/{fileName}";
    }

    public Stream? OpenRead(string relativePath)
    {
        var fullPath = FullPath(relativePath);
        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    }

    public void Delete(string relativePath) => File.Delete(FullPath(relativePath));

    private string FullPath(string relativePath)
    {
        var root = Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Not a stored file: '{relativePath}'.", nameof(relativePath));
        }

        return fullPath;
    }
}
