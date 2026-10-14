using ScreenToGif.Linux.Models;

namespace ScreenToGif.Linux.Services;

/// <summary>
/// Moves a Board recording's frames into the editor's own workspace. The Board owns the
/// workspace it recorded into and deletes it when it is done, so frames inserted into an open
/// project have to be copies the editor owns, not references into somebody else's storage.
/// </summary>
public static class BoardFrameTransfer
{
    public static Task<List<EditorFrame>> CopyIntoAsync(
        EditorWorkspace workspace,
        IReadOnlyList<EditorFrame> source,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => CopyInto(workspace, source, cancellationToken), CancellationToken.None);

    public static List<EditorFrame> CopyInto(
        EditorWorkspace workspace,
        IReadOnlyList<EditorFrame> source,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (source.Count == 0)
            return [];

        var batch = workspace.CreateBatch(EditorArtifactKind.Imports);
        var copies = new List<EditorFrame>(source.Count);

        try
        {
            for (var index = 0; index < source.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = Path.Combine(batch, $"{index:000000}.png");
                File.Copy(source[index].FilePath, path);
                copies.Add(new EditorFrame(path, source[index].DelayMs));
            }
        }
        catch
        {
            foreach (var copy in copies)
                copy.Dispose();
            try
            {
                Directory.Delete(batch, recursive: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            throw;
        }

        return copies;
    }
}
