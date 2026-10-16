namespace QuizPractice.Application.Common;

/// <summary>
/// An upload is stored before the row that points at it, because the row holds its path.
/// When the commit then fails, the file just stored would stay on disk with no row: this
/// removes it and lets the error go on. A file being replaced is never touched here; the
/// service deletes it only after a successful commit, as the row still points at it.
/// </summary>
public static class StoredFile
{
    /// <param name="commit">The save that makes the row point at the file.</param>
    /// <param name="storedPath">The path stored by this request, or null when it stored none.</param>
    /// <param name="delete">IFileStorage.Delete or IPrivateFileStorage.Delete.</param>
    public static async Task CommitAsync(Func<Task> commit, string? storedPath, Action<string> delete)
    {
        try
        {
            await commit();
        }
        catch
        {
            if (storedPath is not null)
            {
                try
                {
                    delete(storedPath);
                }
                catch (IOException)
                {
                    // The commit error matters more than a file left behind.
                }
            }

            throw;
        }
    }
}
