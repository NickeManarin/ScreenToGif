using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class BoardInsertionPlacementTests
{
    [Theory]
    [InlineData(BoardInsertionPosition.BeforeSelected, 2)]
    [InlineData(BoardInsertionPosition.AfterSelected, 3)]
    [InlineData(BoardInsertionPosition.Beginning, 0)]
    [InlineData(BoardInsertionPosition.End, 5)]
    public void ChoiceResolvesAgainstTheSelectedFrame(BoardInsertionPosition choice, int expected) =>
        Assert.Equal(expected, BoardInsertionPlacement.Resolve(5, 2, choice));

    [Fact]
    public void EmptyProjectAcceptsTheRecordingAtTheBeginning() =>
        Assert.Equal(0, BoardInsertionPlacement.Resolve(0, -1, BoardInsertionPosition.End));

    [Fact]
    public void RelativeChoiceRequiresASelectedFrame() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BoardInsertionPlacement.Resolve(5, -1, BoardInsertionPosition.BeforeSelected));

    [Fact]
    public void InsertingKeepsExistingAndRecordedOrder()
    {
        var frames = new List<string> { "old-0", "old-1", "old-2" };

        BoardInsertionPlacement.InsertInto(frames, ["new-0", "new-1"], insertionIndex: 1);

        Assert.Equal(["old-0", "new-0", "new-1", "old-1", "old-2"], frames);
    }

    [Fact]
    public void InvalidInsertionIndexLeavesTheTimelineUntouched()
    {
        var frames = new List<string> { "old-0" };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BoardInsertionPlacement.InsertInto(frames, ["new-0"], insertionIndex: 2));

        Assert.Equal(["old-0"], frames);
    }
}
