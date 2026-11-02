namespace ScreenToGif.Linux.Services;

public static class FrameSequenceOperations
{
    public static void InsertAfter<T>(IList<T> items, int boundaryIndex, IReadOnlyList<T> inserted)
    {
        if (boundaryIndex < 0 || boundaryIndex >= items.Count)
            throw new ArgumentOutOfRangeException(nameof(boundaryIndex));
        for (var index = 0; index < inserted.Count; index++)
            items.Insert(boundaryIndex + 1 + index, inserted[index]);
    }

    public static IReadOnlyList<FrameState> Delete(
        IReadOnlyList<FrameState> frames,
        IEnumerable<int> indices) =>
        FilterOut(frames, Normalize(indices, frames.Count));

    public static IReadOnlyList<FrameState> DeleteBefore(IReadOnlyList<FrameState> frames, int boundaryIndex) =>
        boundaryIndex <= 0 ? frames.ToArray() : frames.Skip(Math.Min(boundaryIndex, frames.Count)).ToArray();

    public static IReadOnlyList<FrameState> DeleteAfter(IReadOnlyList<FrameState> frames, int boundaryIndex) =>
        boundaryIndex < 0 ? frames.ToArray() : frames.Take(Math.Min(boundaryIndex + 1, frames.Count)).ToArray();

    public static IReadOnlyList<FrameState> Reverse(
        IReadOnlyList<FrameState> frames,
        IEnumerable<int> indices)
    {
        var result = frames.ToArray();
        var normalized = Normalize(indices, frames.Count);
        var reversed = normalized.Select(index => frames[index]).Reverse().ToArray();
        for (var index = 0; index < normalized.Length; index++)
            result[normalized[index]] = reversed[index];
        return result;
    }

    public static IReadOnlyList<FrameState> Move(
        IReadOnlyList<FrameState> frames,
        IEnumerable<int> indices,
        int offset)
    {
        var result = frames.ToArray();
        var selected = Normalize(indices, frames.Count).ToHashSet();
        if (offset < 0)
        {
            for (var index = 1; index < result.Length; index++)
                if (selected.Contains(index) && !selected.Contains(index - 1))
                {
                    (result[index - 1], result[index]) = (result[index], result[index - 1]);
                    selected.Remove(index);
                    selected.Add(index - 1);
                }
        }
        else if (offset > 0)
        {
            for (var index = result.Length - 2; index >= 0; index--)
                if (selected.Contains(index) && !selected.Contains(index + 1))
                {
                    (result[index], result[index + 1]) = (result[index + 1], result[index]);
                    selected.Remove(index);
                    selected.Add(index + 1);
                }
        }

        return result;
    }

    public static IReadOnlyList<int> MoveSelection(int frameCount, IEnumerable<int> indices, int offset)
    {
        var selected = Normalize(indices, frameCount).ToHashSet();
        if (offset < 0)
        {
            for (var index = 1; index < frameCount; index++)
                if (selected.Contains(index) && !selected.Contains(index - 1))
                {
                    selected.Remove(index);
                    selected.Add(index - 1);
                }
        }
        else if (offset > 0)
        {
            for (var index = frameCount - 2; index >= 0; index--)
                if (selected.Contains(index) && !selected.Contains(index + 1))
                {
                    selected.Remove(index);
                    selected.Add(index + 1);
                }
        }

        return selected.Order().ToArray();
    }

    public static IReadOnlyList<FrameState> Reduce(
        IReadOnlyList<FrameState> frames,
        IEnumerable<int> indices,
        int factor)
    {
        if (factor < 2)
            throw new ArgumentOutOfRangeException(nameof(factor), "Reduction factor must be at least 2.");
        var normalized = Normalize(indices, frames.Count);
        var remove = normalized.Where((_, ordinal) => ordinal % factor != 0).ToArray();
        return FilterOut(frames, remove);
    }

    public static IReadOnlyList<FrameState> Yoyo(
        IReadOnlyList<FrameState> frames,
        IEnumerable<int> indices)
    {
        var normalized = Normalize(indices, frames.Count);
        if (normalized.Length < 3)
            return frames.ToArray();
        var result = frames.ToList();
        var insertAt = normalized[^1] + 1;
        var interiorReverse = normalized.Skip(1).SkipLast(1).Reverse().Select(index => frames[index]);
        result.InsertRange(insertAt, interiorReverse);
        return result;
    }

    public static IReadOnlyList<FrameState> ApplyDelay(
        IReadOnlyList<FrameState> frames,
        IEnumerable<int> indices,
        int delayMs)
    {
        if (delayMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(delayMs));
        var selected = Normalize(indices, frames.Count).ToHashSet();
        return frames.Select((frame, index) => selected.Contains(index) ? frame with { DelayMs = delayMs } : frame).ToArray();
    }

    public static async Task<IReadOnlyList<int>> FindConsecutiveDuplicatesAsync(
        IReadOnlyList<FrameState> frames,
        IEnumerable<int> scopeIndices,
        IFfmpegTool ffmpeg,
        CancellationToken cancellationToken = default)
    {
        var scope = Normalize(scopeIndices, frames.Count).ToHashSet();
        var duplicateIndices = new List<int>();
        var hashes = new string?[frames.Count];
        string? previousHash = null;
        var previousWasInScope = false;

        await Parallel.ForEachAsync(
            scope,
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
            async (index, token) =>
        {
            var result = await ffmpeg.RunFfmpegAsync(
            [
                "-hide_banner", "-loglevel", "error", "-i", frames[index].FilePath,
                "-frames:v", "1", "-pix_fmt", "rgba", "-f", "hash", "-hash", "sha256", "-"
            ], token);
            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Could not compare frame {index + 1}: {result.StandardError.Trim()}");
            hashes[index] = result.StandardOutput.Trim();
        });

        for (var index = 0; index < frames.Count; index++)
        {
            if (!scope.Contains(index))
            {
                previousHash = null;
                previousWasInScope = false;
                continue;
            }

            var hash = hashes[index]!;
            if (previousWasInScope && string.Equals(previousHash, hash, StringComparison.Ordinal))
                duplicateIndices.Add(index);
            else
                previousHash = hash;
            previousWasInScope = true;
        }

        return duplicateIndices;
    }

    private static IReadOnlyList<FrameState> FilterOut(IReadOnlyList<FrameState> frames, IEnumerable<int> indices)
    {
        var removed = indices.ToHashSet();
        return frames.Where((_, index) => !removed.Contains(index)).ToArray();
    }

    private static int[] Normalize(IEnumerable<int> indices, int frameCount) =>
        indices.Where(index => index >= 0 && index < frameCount).Distinct().Order().ToArray();
}
