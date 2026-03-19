using ScreenToGif.Controls;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Views.Settings;

public partial class ShortcutsSettings : Page
{
    public ShortcutsSettings()
    {
        InitializeComponent();
    }

    private void ShortcutsSettings_Loaded(object sender, RoutedEventArgs e)
    {
        Global.IgnoreHotKeys = true;
    }

    private void Globals_OnKeyChanged(object sender, KeyChangedEventArgs e)
    {
        Recorders_OnKeyChanged(sender, e);

        if (e.Cancel)
            return;

        //Unregister old shortcut.
        HotKeyCollection.Default.Remove(e.PreviousModifiers, e.PreviousKey);

        //Registers all shortcuts and updates the input gesture text.
        App.RegisterShortcuts();
    }

    private void Recorders_OnKeyChanged(object sender, KeyChangedEventArgs e)
    {
        if (sender is not KeyBox box)
            return;

        var list = new List<Tuple<Key, ModifierKeys>>
        {
            new(UserSettings.All.RecorderShortcut, UserSettings.All.RecorderModifiers),
            new(UserSettings.All.BoardRecorderShortcut, UserSettings.All.BoardRecorderModifiers),
            new(UserSettings.All.WebcamRecorderShortcut, UserSettings.All.WebcamRecorderModifiers),
            new(UserSettings.All.EditorShortcut, UserSettings.All.EditorModifiers),
            new(UserSettings.All.OptionsShortcut, UserSettings.All.OptionsModifiers),
            new(UserSettings.All.ExitShortcut, UserSettings.All.ExitModifiers),
            new(UserSettings.All.StartPauseShortcut, UserSettings.All.StartPauseModifiers),
            new(UserSettings.All.StopShortcut, UserSettings.All.StopModifiers),
            new(UserSettings.All.DiscardShortcut, UserSettings.All.DiscardModifiers)
        };

        //If this new shortcut is already in use.
        if (box.MainKey != Key.None && list.Count(c => c.Item1 == box.MainKey && c.Item2 == box.ModifierKeys) > 1)
        {
            box.MainKey = e.PreviousKey;
            box.ModifierKeys = e.PreviousModifiers;
            e.Cancel = true;
        }
    }
    
    private void ShortcutsSettings_Unloaded(object sender, RoutedEventArgs e)
    {
        Global.IgnoreHotKeys = false;
    }
}