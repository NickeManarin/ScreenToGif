namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// The moment a finished recording changes hands. Until the editor has taken ownership the
/// recording is still the recorder's, so a hand-off that throws has to delete it: frames left on
/// disk with nothing able to reach them are neither the editor's nor recoverable.
///
/// This lives outside the recorder window so the failure path can be exercised without a display.
/// </summary>
public static class RecordingHandOff
{
    /// <summary>
    /// Offers <paramref name="project"/> to <paramref name="takeOwnership"/>. Returns null once the
    /// editor holds it; on failure the recording is disposed and the cause is returned, so the
    /// caller can say what happened rather than having to know what to clean up.
    /// </summary>
    public static async Task<Exception?> TransferAsync(LoadedProject project, Func<LoadedProject, Task> takeOwnership)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(takeOwnership);

        try
        {
            await takeOwnership(project);
            return null;
        }
        catch (Exception exception)
        {
            project.Dispose();
            return exception;
        }
    }
}
