namespace ScreenToGif.Linux.Services;

/// <summary>The position chosen when a Board recording joins an open editor project.</summary>
public enum BoardInsertionPosition
{
    BeforeSelected,
    AfterSelected,
    Beginning,
    End
}

public static class BoardInsertionPlacement
{
    public static int Resolve(int frameCount, int selectedIndex, BoardInsertionPosition position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frameCount);

        return position switch
        {
            BoardInsertionPosition.Beginning => 0,
            BoardInsertionPosition.End => frameCount,
            BoardInsertionPosition.BeforeSelected when selectedIndex >= 0 && selectedIndex < frameCount => selectedIndex,
            BoardInsertionPosition.AfterSelected when selectedIndex >= 0 && selectedIndex < frameCount => selectedIndex + 1,
            _ => throw new ArgumentOutOfRangeException(nameof(selectedIndex), "Select a frame before choosing a relative position.")
        };
    }

    public static void InsertInto<T>(IList<T> frames, IReadOnlyList<T> incoming, int insertionIndex)
    {
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(incoming);
        if (insertionIndex < 0 || insertionIndex > frames.Count)
            throw new ArgumentOutOfRangeException(nameof(insertionIndex));

        for (var offset = 0; offset < incoming.Count; offset++)
            frames.Insert(insertionIndex + offset, incoming[offset]);
    }
}
