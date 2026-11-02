using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Covers the FFmpeg V4L2 format listing parser and the capture-mode choice, both of which are pure
/// text handling and need neither a camera nor a child process.
/// </summary>
public sealed class CameraFormatCatalogTests
{
    private const string Listing = """
        ffmpeg version 6.1.1-3ubuntu5 Copyright (c) 2000-2023 the FFmpeg developers
          libavutil      58. 29.100 / 58. 29.100
        [video4linux2,v4l2 @ 0x5591e8b56680] Compressed:       mjpeg :          Motion-JPEG : 2560x1440 1920x1080 1280x720 640x480
        [video4linux2,v4l2 @ 0x5591e8b56680] Raw       :     yuyv422 :           YUYV 4:2:2 : 1280x720 640x480 320x240
        /dev/video0: Immediate exit requested
        """;

    [Fact]
    public void CompressedAndRawLinesParseWithTheirInputFormatFlagAndEveryListedSize()
    {
        var formats = CameraFormatCatalog.Parse(Listing);

        Assert.Equal(
            [
                new CameraCaptureFormat("mjpeg", IsCompressed: true, 2560, 1440),
                new CameraCaptureFormat("mjpeg", IsCompressed: true, 1920, 1080),
                new CameraCaptureFormat("mjpeg", IsCompressed: true, 1280, 720),
                new CameraCaptureFormat("mjpeg", IsCompressed: true, 640, 480),
                new CameraCaptureFormat("yuyv422", IsCompressed: false, 1280, 720),
                new CameraCaptureFormat("yuyv422", IsCompressed: false, 640, 480),
                new CameraCaptureFormat("yuyv422", IsCompressed: false, 320, 240)
            ],
            formats);
    }

    [Fact]
    public void RepeatedFormatAndSizePairsAreCollapsed()
    {
        const string repeated = """
            [video4linux2,v4l2 @ 0x5591e8b56680] Compressed:       mjpeg :          Motion-JPEG : 1280x720 1280x720 640x480
            [video4linux2,v4l2 @ 0x5591e8b56680] Compressed:       mjpeg :          Motion-JPEG : 640x480
            """;

        Assert.Equal(
            [
                new CameraCaptureFormat("mjpeg", IsCompressed: true, 1280, 720),
                new CameraCaptureFormat("mjpeg", IsCompressed: true, 640, 480)
            ],
            CameraFormatCatalog.Parse(repeated));
    }

    [Fact]
    public void OutputWithoutAnyFormatListingLineParsesToNothing()
    {
        const string noise = """
            ffmpeg version 6.1.1-3ubuntu5 Copyright (c) 2000-2023 the FFmpeg developers
            [video4linux2,v4l2 @ 0x5591e8b56680] Cannot open video device /dev/video9: No such file or directory
            /dev/video9: No such file or directory
            """;

        Assert.Empty(CameraFormatCatalog.Parse(noise));
    }

    [Fact]
    public void AStepwiseSizeRangeExpandsToTheBoundedCandidateSizes()
    {
        const string stepwise =
            "[video4linux2,v4l2 @ 0x5591e8b56680] Raw       :     yuyv422 :           YUYV 4:2:2 : {32-2560, 2}x{32-1440, 2}";

        Assert.Equal(
            [
                new CameraCaptureFormat("yuyv422", IsCompressed: false, 1920, 1080),
                new CameraCaptureFormat("yuyv422", IsCompressed: false, 1280, 720),
                new CameraCaptureFormat("yuyv422", IsCompressed: false, 640, 480),
                new CameraCaptureFormat("yuyv422", IsCompressed: false, 320, 240)
            ],
            CameraFormatCatalog.Parse(stepwise));
    }

    [Fact]
    public void ChooseTakesTheLargestModeThatFitsTheBounds()
    {
        var chosen = CameraFormatCatalog.Choose(CameraFormatCatalog.Parse(Listing));

        Assert.Equal(new CameraCaptureFormat("mjpeg", IsCompressed: true, 1280, 720), chosen);
    }

    [Fact]
    public void ChoosePrefersTheCompressedFormatWhenTwoModesTieOnSize()
    {
        CameraCaptureFormat[] formats =
        [
            new("yuyv422", IsCompressed: false, 1280, 720),
            new("mjpeg", IsCompressed: true, 1280, 720)
        ];

        Assert.Equal(new CameraCaptureFormat("mjpeg", IsCompressed: true, 1280, 720), CameraFormatCatalog.Choose(formats));
    }

    [Fact]
    public void ChooseFallsBackToTheSmallestModeWhenEverythingExceedsTheBounds()
    {
        CameraCaptureFormat[] formats =
        [
            new("mjpeg", IsCompressed: true, 3840, 2160),
            new("yuyv422", IsCompressed: false, 1920, 1080),
            new("mjpeg", IsCompressed: true, 2560, 1440)
        ];

        Assert.Equal(
            new CameraCaptureFormat("yuyv422", IsCompressed: false, 1920, 1080),
            CameraFormatCatalog.Choose(formats));
    }

    [Theory]
    [InlineData("h264")]
    [InlineData("hevc")]
    [InlineData("vp8")]
    [InlineData("vp9")]
    [InlineData("av1")]
    public void ChooseSkipsVideoCodecModesInFavourOfADecodableOne(string codec)
    {
        CameraCaptureFormat[] formats =
        [
            new(codec, IsCompressed: true, 1280, 720),
            new("yuyv422", IsCompressed: false, 640, 480)
        ];

        Assert.Equal(
            new CameraCaptureFormat("yuyv422", IsCompressed: false, 640, 480),
            CameraFormatCatalog.Choose(formats));
        Assert.Null(CameraFormatCatalog.Choose([new CameraCaptureFormat(codec, IsCompressed: true, 1280, 720)]));
    }

    [Fact]
    public void ChooseOnAnEmptySequenceReturnsNothing()
    {
        Assert.Null(CameraFormatCatalog.Choose([]));
    }

    [Fact]
    public void SizesOfferEachUsableResolutionOnceIncludingModesAboveTheInitialBound()
    {
        var formats = CameraFormatCatalog.Parse(Listing);
        var sizes = CameraFormatCatalog.Sizes(formats);

        Assert.Equal(
            [new CameraResolution(2560, 1440), new(1920, 1080), new(1280, 720), new(640, 480), new(320, 240)],
            sizes);
        Assert.Equal(new CameraCaptureFormat("mjpeg", true, 1920, 1080),
            CameraFormatCatalog.ChooseAtSize(formats, new CameraResolution(1920, 1080)));
    }

    [Fact]
    public void StoredSizeWinsAndDisappearingSizeFallsBackToConcreteInitialChoice()
    {
        var formats = CameraFormatCatalog.Parse(Listing);
        Assert.Equal(new CameraResolution(1280, 720), CameraFormatCatalog.SelectSize(formats, null));
        Assert.Equal(new CameraResolution(1920, 1080),
            CameraFormatCatalog.SelectSize(formats, new CameraResolution(1920, 1080)));
        Assert.Equal(new CameraResolution(1280, 720),
            CameraFormatCatalog.SelectSize(formats, new CameraResolution(3840, 2160)));
    }

    [Fact]
    public void SizesExcludeModesTheEditorCannotAcceptWithoutChangingItsLimit()
    {
        CameraCaptureFormat[] formats =
        [
            new("mjpeg", true, EditorResourceLimits.MaximumMediaDimension + 1, 1080),
            new("mjpeg", true, 3840, 2160)
        ];

        Assert.Equal([new CameraResolution(3840, 2160)], CameraFormatCatalog.Sizes(formats));
        Assert.Equal(new CameraResolution(3840, 2160), CameraFormatCatalog.SelectSize(formats, null));
    }

    [Fact]
    public void ListArgumentsAskV4l2ForEveryFormatTheDeviceOffers()
    {
        Assert.Equal(
            ["-hide_banner", "-f", "v4l2", "-list_formats", "all", "-i", "/dev/video0"],
            CameraFormatCatalog.ListArguments("/dev/video0"));
    }
}
