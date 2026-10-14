using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

public partial class MainWindow
{
    private readonly DispatcherTimer _previewTimer = new();
    private readonly PlaybackSession _playback = new(new StopwatchPlaybackClock());

    partial void InitializePlayback()
    {
        DropFramesCheckBox.IsChecked = LinuxSettings.Current.DropFramesWhenBehind;
        _previewTimer.Tick += PreviewTimerTick;
        _stopActivePreview = StopPlaybackPreview;
        OperationStateChanged += UpdatePlaybackControls;
        FrameInfoUpdated += UpdatePlaybackControls;
        CleanupRequested += StopPlaybackPreview;
    }

    private void PlayClick(object? sender, RoutedEventArgs e)
    {
        if (_operations.IsRunning)
        {
            SetStatus("Wait for the active operation to finish, or use Stop to cancel it.");
            return;
        }

        if (_playback.IsPlaying)
        {
            StopPreview();
            SetStatus("Preview stopped.");
            return;
        }

        if (_frames.Count == 0)
        {
            SetStatus("There are no frames to preview.");
            return;
        }

        _playback.Start(_currentFrameIndex);
        _deferSelectionPreview = true;
        RibbonPlayButton.Label = "Stop";
        RibbonPlayButton.IconKind = "Stop";
        StatusPlayIcon.Kind = "Stop";
        PreviewTimerTick(this, EventArgs.Empty);
        SetStatus("Playing preview...");
    }

    private void StopClick(object? sender, RoutedEventArgs e)
    {
        if (_operations.RequestCancellation())
        {
            SetStatus("Canceling the active operation...");
            return;
        }

        StopPreview();
        SetStatus("Preview stopped.");
    }

    private void FirstClick(object? sender, RoutedEventArgs e) => NavigateTo(PlaybackTimeline.First(_frames.Count));
    private void PreviousClick(object? sender, RoutedEventArgs e) => NavigateTo(PlaybackTimeline.Previous(_currentFrameIndex, _frames.Count));
    private void NextClick(object? sender, RoutedEventArgs e) => NavigateTo(PlaybackTimeline.Next(_currentFrameIndex, _frames.Count));
    private void LastClick(object? sender, RoutedEventArgs e) => NavigateTo(PlaybackTimeline.Last(_frames.Count));

    private void NavigateTo(int index)
    {
        StopPreview();
        if (index < 0)
        {
            SetStatus("There are no frames to navigate through.");
            return;
        }

        FrameListBox.SelectedItems?.Clear();
        FrameListBox.SelectedIndex = index;
        FrameListBox.ScrollIntoView(_frames[index]);
        UpdateCurrentFramePreview();
        SetStatus($"Selected frame {index + 1} of {_frames.Count}.");
    }

    private void PreviewTimerTick(object? sender, EventArgs e)
    {
        if (!_playback.IsPlaying || _frames.Count == 0)
        {
            StopPreview();
            return;
        }

        var step = _playback.Advance(
            _frames.Select(frame => frame.DelayMs).ToArray(),
            LoopPlaybackCheckBox.IsChecked == true,
            DropFramesCheckBox.IsChecked == true);
        if (step.Completed)
        {
            StopPreview();
            SetCurrentFrameIndex(step.FrameIndex);
            if (step.FrameIndex >= 0)
            {
                var completedFrame = _frames[step.FrameIndex];
                SetPreview(completedFrame);
                FrameListBox.ScrollIntoView(completedFrame);
            }

            UpdateFrameInfo();
            SetStatus("Preview completed.");
            return;
        }

        var frame = _frames[step.FrameIndex];
        SetCurrentFrameIndex(step.FrameIndex);
        SetPreview(frame);
        UpdateFrameInfo();
        _previewTimer.Interval = TimeSpan.FromMilliseconds(step.DelayMs);
        _previewTimer.Start();
    }

    private void StopPlaybackPreview()
    {
        _playback.Stop();
        _deferSelectionPreview = false;
        _previewTimer.Stop();
        RibbonPlayButton.Label = "Play";
        RibbonPlayButton.IconKind = "Play";
        StatusPlayIcon.Kind = "Play";
        UpdatePlaybackControls();
    }

    private void UpdatePlaybackControls()
    {
        var state = PlaybackControlState.Calculate(_frames.Count, _currentFrameIndex, _operations.IsRunning);
        FirstPlaybackButton.IsEnabled = state.FirstEnabled;
        PreviousPlaybackButton.IsEnabled = state.PreviousEnabled;
        NextPlaybackButton.IsEnabled = state.NextEnabled;
        LastPlaybackButton.IsEnabled = state.LastEnabled;
        RibbonPlayButton.IsEnabled = state.PlayEnabled;
        StatusPlayButton.IsEnabled = state.PlayEnabled;
        LoopPlaybackCheckBox.IsEnabled = state.OptionsEnabled;
        DropFramesCheckBox.IsEnabled = state.OptionsEnabled;

        var noFrames = _frames.Count == 0;
        ToolTip.SetTip(FirstPlaybackButton, noFrames ? "Open media to enable navigation." :
            state.FirstEnabled ? "Go to the first frame." : "Already at the first frame.");
        ToolTip.SetTip(PreviousPlaybackButton, noFrames ? "Open media to enable navigation." :
            state.PreviousEnabled ? "Go to the previous frame." : "Already at the first frame.");
        ToolTip.SetTip(NextPlaybackButton, noFrames ? "Open media to enable navigation." :
            state.NextEnabled ? "Go to the next frame." : "Already at the last frame.");
        ToolTip.SetTip(LastPlaybackButton, noFrames ? "Open media to enable navigation." :
            state.LastEnabled ? "Go to the last frame." : "Already at the last frame.");
        var playTip = noFrames ? "Open media to enable playback." :
            _operations.IsRunning ? "Wait for the active operation to finish or use Stop to cancel it." :
            _playback.IsPlaying ? "Stop the preview." : "Play the preview.";
        ToolTip.SetTip(RibbonPlayButton, playTip);
        ToolTip.SetTip(StatusPlayButton, playTip);
        var optionsTip = noFrames ? "Open media to enable playback options." : "Configure preview playback.";
        ToolTip.SetTip(LoopPlaybackCheckBox, optionsTip);
        ToolTip.SetTip(DropFramesCheckBox, optionsTip);
    }
}
