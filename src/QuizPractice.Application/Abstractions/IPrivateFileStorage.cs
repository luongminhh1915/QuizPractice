namespace QuizPractice.Application.Abstractions;

/// <summary>
/// Stores uploads that must not be public, such as question media (SDS I.1: kept outside
/// wwwroot, in App_Data). Nothing serves these files directly: a controller streams one
/// after checking who asks (BR-05). Like IFileStorage, a file always gets a new random name.
/// </summary>
public interface IPrivateFileStorage
{
    /// <summary>Saves the content under {folder}/ and returns its relative path, e.g. question-media/7f3a9c2e.mp4.</summary>
    Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default);

    /// <summary>Opens a stored file for reading, or null when it is missing.</summary>
    Stream? OpenRead(string relativePath);

    /// <summary>Removes a stored file. A missing file is not an error.</summary>
    void Delete(string relativePath);
}
