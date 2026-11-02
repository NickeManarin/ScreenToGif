using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// The rule deciding which of the recorder's controls are usable. It lives outside the window so the
/// combinations that used to disagree with each other are checked here rather than by clicking.
/// </summary>
public sealed class WebcamControlsTests
{
    private static readonly WebcamStatus NoCamera = WebcamStatus.FromDiscovery(CameraDiscoveryResult.Empty);

    private static readonly WebcamStatus Unreadable =
        WebcamStatus.FromStreamFailure(new CameraStreamFailure("/dev/video0", 240, "Device or resource busy"));

    private static WebcamControls Idle(
        bool openInFlight = false,
        bool hasStream = true,
        bool hasPendingRecording = false,
        WebcamStatus? status = null) =>
        WebcamControls.For(
            status ?? WebcamStatus.Ready,
            WebcamRecordingStage.Stopped,
            openInFlight,
            hasStream,
            hasFrames: false,
            hasPendingRecording);

    [Fact]
    public void AReadyCameraOffersEverythingAndNothingToStopYet()
    {
        var controls = Idle();

        Assert.True(controls.CanRecord);
        Assert.True(controls.CanChooseDevice);
        Assert.True(controls.CanRefresh);
        Assert.True(controls.CanChangeFrameRate);
        Assert.True(controls.CanScale);
        Assert.True(controls.CanOpenOptions);
        Assert.False(controls.CanStop);
        Assert.False(controls.ShowDiscard);
        Assert.False(controls.ShowRetry);
    }

    [Fact]
    public void RecordingCannotStartWhileACameraIsStillBeingOpened()
    {
        var controls = Idle(openInFlight: true);

        Assert.False(controls.CanRecord);
        Assert.False(controls.CanChooseDevice);
        Assert.False(controls.CanRefresh);
        Assert.False(controls.CanChangeFrameRate);
        Assert.False(controls.CanScale);
        Assert.False(controls.ShowRetry);
    }

    [Fact]
    public void RecordingCannotStartWithNoStreamBehindIt() => Assert.False(Idle(hasStream: false).CanRecord);

    [Fact]
    public void AFailedStateOffersAWayToLookAgainAndNoWayToRecord()
    {
        var controls = Idle(hasStream: false, status: Unreadable);

        Assert.False(controls.CanRecord);
        Assert.True(controls.CanRefresh);
        Assert.True(controls.CanChooseDevice);
        Assert.True(controls.ShowRetry);
    }

    [Fact]
    public void WithNoCameraAtAllTheDeviceSelectorIsOfferedNoMoreThanRecordingIs()
    {
        var controls = Idle(hasStream: false, status: NoCamera);

        Assert.False(controls.CanRecord);
        Assert.False(controls.CanChooseDevice);
        Assert.False(controls.CanChangeFrameRate);
        Assert.True(controls.CanRefresh);
        Assert.True(controls.ShowRetry);
    }

    [Theory]
    [InlineData(WebcamRecordingStage.Recording)]
    [InlineData(WebcamRecordingStage.Paused)]
    public void ARunningRecordingLocksEverythingThatWouldChangeWhatItIsRecording(WebcamRecordingStage stage)
    {
        var controls = WebcamControls.For(
            WebcamStatus.Ready, stage, openInFlight: false, hasStream: true, hasFrames: true, hasPendingRecording: false);

        Assert.True(controls.CanRecord);
        Assert.True(controls.CanStop);
        Assert.True(controls.ShowDiscard);
        Assert.False(controls.CanChooseDevice);
        Assert.False(controls.CanRefresh);
        Assert.False(controls.CanChangeFrameRate);
        Assert.False(controls.CanScale);
        Assert.False(controls.CanOpenOptions);
    }

    [Fact]
    public void ARecordingWithNoFramesYetCannotBeStopped() =>
        Assert.False(WebcamControls.For(
            WebcamStatus.Ready, WebcamRecordingStage.Recording, false, true, hasFrames: false, hasPendingRecording: false).CanStop);

    [Fact]
    public void ARecordingWaitingToBeHandedOverKeepsStopAndDiscardAndWillNotBeStartedOver()
    {
        var controls = Idle(hasPendingRecording: true);

        Assert.True(controls.CanStop);
        Assert.True(controls.ShowDiscard);
        Assert.False(controls.CanRecord);
    }
}
