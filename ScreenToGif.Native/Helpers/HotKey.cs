using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Interop;
using ScreenToGif.Native.External;

namespace ScreenToGif.Native.Helpers;

public class HotKey : IDisposable
{
    #region Variables

    private const int WmHotKey = 0x0312;

    private int _id;
    private readonly IntPtr _windowHandle;
    private readonly Action _callback;

    #endregion

    #region Properties

    internal ModifierKeys Modifier { get; }

    internal Key Key { get; }

    #endregion


    public HotKey(ModifierKeys modifier, Key key, IntPtr windowsHandle, Action callback)
    {
        Modifier = modifier;
        Key = key;

        var keys = ConvertWinformsToWpfKey(key);

        _windowHandle = windowsHandle;
        _id = GetHashCode();
        _callback = callback;

        if (!User32.RegisterHotKey(_windowHandle, _id, Modifier, keys))
            throw new InvalidOperationException("Hotkey already in use");

        ComponentDispatcher.ThreadPreprocessMessage += ThreadPreprocessMessageMethod;
    }

    public HotKey(ModifierKeys modifier, Key key, Action callback, bool unregisterFirst = false)
    {
        Modifier = modifier;
        Key = key;

        var keys = ConvertWinformsToWpfKey(key);

        _windowHandle = IntPtr.Zero;
        _id = GetHashCode();
        _callback = callback;

        if (unregisterFirst)
            User32.UnregisterHotKey(_windowHandle, _id);

        if (!User32.RegisterHotKey(_windowHandle, _id, Modifier, keys))
            throw new InvalidOperationException("Hotkey already in use");

        ComponentDispatcher.ThreadPreprocessMessage += ThreadPreprocessMessageMethod;
    }

    public sealed override int GetHashCode()
    {
        unchecked
        {
            return ((int)Modifier * 397) ^ (int)Key;
        }
    }

    private static Keys ConvertWinformsToWpfKey(Key inputKey)
    {
        try
        {
            return (Keys)Enum.Parse(typeof(Keys), inputKey.ToString());
        }
        catch
        {
            return Keys.None;
        }
    }

    [DebuggerStepThrough]
    private void ThreadPreprocessMessageMethod(ref MSG msg, ref bool handled)
    {
        if (handled || msg.message != WmHotKey || (int)msg.wParam != _id)
            return;

        _callback.Invoke();

        handled = true;
    }

    public void Dispose()
    {
        if (_id <= 0) return;

        if (!User32.UnregisterHotKey(_windowHandle, _id))
        {
            //TODO: Warning?
        }

        _id = 0;
    }
}