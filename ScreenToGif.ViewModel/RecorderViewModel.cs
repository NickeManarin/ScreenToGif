using System.Windows.Input;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.ViewModel;

public class RecorderViewModel : BindableBase
{
    #region Properties

    private RecorderStages _stage = RecorderStages.Stopped;

    public RecorderStages Stage
    {
        get => _stage;
        set => SetProperty(ref _stage, value);
    }

    #endregion

    #region Commands

    private KeyGesture _recordKeyGesture = null;
    private KeyGesture _stopKeyGesture = null;
    private KeyGesture _discardKeyGesture = null;

    public KeyGesture RecordKeyGesture
    {
        get => _recordKeyGesture;
        set => SetProperty(ref _recordKeyGesture, value);
    }

    public KeyGesture StopKeyGesture
    {
        get => _stopKeyGesture;
        set => SetProperty(ref _stopKeyGesture, value);
    }

    public KeyGesture DiscardKeyGesture
    {
        get => _discardKeyGesture;
        set => SetProperty(ref _discardKeyGesture, value);
    }


    public RoutedUICommand CloseCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.Close"
    };

    public RoutedUICommand OptionsCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.Options",
        InputGestures = { new KeyGesture(UserSettings.All.OptionsShortcut, UserSettings.All.OptionsModifiers) }
    };

    public RoutedUICommand RecordCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.Record"
    };

    public RoutedUICommand SnapCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.Snap"
    };

    public RoutedUICommand PauseCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.PauseCapture"
    };

    public RoutedUICommand StopCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.StopCapture"
    };

    public RoutedUICommand StopLargeCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.StopCapture"
    };

    public RoutedUICommand DiscardCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.DiscardCapture"
    };

    public RoutedUICommand SwitchFrequencyCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.SwitchCaptureFrequency",
    };

    public RoutedUICommand SnapToWindowCommand { get; set; } = new RoutedUICommand
    {
        Text = "S.Command.SnapToWindow",
    };

    #endregion

    public void RefreshKeyGestures()
    {
        try
        {
            RecordKeyGesture = new KeyGesture(UserSettings.All.StartPauseShortcut, UserSettings.All.StartPauseModifiers);
            StopKeyGesture = new KeyGesture(UserSettings.All.StopShortcut, UserSettings.All.StopModifiers);
            DiscardKeyGesture = new KeyGesture(UserSettings.All.DiscardShortcut, UserSettings.All.DiscardModifiers);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to set the key gestures for the recorder.");
        }
    }
}