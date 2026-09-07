using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class FrameDeletionHistoryTests
{
    [Fact]
    public void Restores_deleted_frames_in_their_original_order_and_selection()
    {
        var frames = new List<EditorFrame>
        {
            new("frame-0.png", 100),
            new("frame-1.png", 120),
            new("frame-2.png", 140),
            new("frame-3.png", 160)
        };
        var deleted = new[] { frames[1], frames[3] };
        var history = new FrameDeletionHistory();

        history.Record(deleted, [1, 3], deleted, selectedIndex: 3);
        frames.RemoveAt(3);
        frames.RemoveAt(1);

        Assert.True(history.TryRestore(frames, out var restored));
        Assert.Equal(
            ["frame-0.png", "frame-1.png", "frame-2.png", "frame-3.png"],
            frames.Select(frame => frame.FilePath));
        Assert.Equal(deleted, restored.SelectedFrames);
        Assert.Equal(3, restored.SelectedIndex);
    }
}
