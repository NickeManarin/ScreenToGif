namespace ScreenToGif.Linux.Services;

public static class EditorResourceLimits
{
    public const int MaximumMediaDimension = 8192;
    public const int MaximumProjectFrames = 10_000;
    public const long MaximumImportedPixels = 1_000_000_000;
    public const long MaximumProjectManifestBytes = 1_048_576;
    public const long MaximumProjectFrameBytes = 300_000_000;
    public const long MaximumProjectArchiveBytes = 4_294_967_296;

    /// <summary>
    /// The most frames of one size a project can hold and still be saved: bounded by the decoded-pixel
    /// limit as well as the frame limit. A recorder stops growing here rather than handing the editor a
    /// project that loads and then cannot be saved; every frame of a recording is the same size, so the
    /// bound is known before the first frame is taken.
    /// </summary>
    public static int MaximumFramesAt(int width, int height)
    {
        var area = Math.Max(1L, (long)Math.Max(1, width) * Math.Max(1, height));
        return (int)Math.Clamp(MaximumImportedPixels / area, 1, MaximumProjectFrames);
    }

    public static void EnsureCanInsertFrames(int currentFrameCount, int additionalFrameCount, string operation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(currentFrameCount);
        ArgumentOutOfRangeException.ThrowIfNegative(additionalFrameCount);

        if (currentFrameCount > MaximumProjectFrames ||
            additionalFrameCount > MaximumProjectFrames - currentFrameCount)
            throw new InvalidOperationException($"{operation} would exceed the {MaximumProjectFrames}-frame project limit; remove frames or add fewer frames.");
    }
}
