using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.External;
using System.Text;
using System.Windows.Input;

namespace ScreenToGif.Util.Helpers;

public static class KeyHelper
{
    public static char? GetCharFromKey(Key key, bool ignoreState = true)
    {
        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        var keyboardState = new byte[256];

        if (!ignoreState)
            User32.GetKeyboardState(keyboardState);

        var scanCode = User32.MapVirtualKey((uint)virtualKey, MapTypes.MapvkVkToVsc);
        var stringBuilder = new StringBuilder(2);

        var result = User32.ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);

        switch (result)
        {
            case 0:
                break;
            default: //Case 1
                return stringBuilder[0];
        }

        return null;
    }

    public static string GetSelectKeyText(Key key, ModifierKeys modifier = ModifierKeys.None, bool isUppercase = false, bool ignoreNone = false, bool translate = false)
    {
        if (ignoreNone && key == Key.None)
            return "";

        //Get the modifiers as text.
        var modifiersText = Enum.GetValues(modifier.GetType()).OfType<ModifierKeys>()
            .Where(x => x != ModifierKeys.None && modifier.HasFlag(x))
            .Aggregate("", (current, mod) =>
            {
                if (mod == ModifierKeys.Control) //TODO: Custom mod.ToString();
                    return current + "Ctrl" + " + ";

                return current + mod + " + ";
            });

        var result = GetCharFromKey(key);

        if (result == null || string.IsNullOrWhiteSpace(result.ToString()) || result < 32)
        {
            //Some keys need to be displayed differently.
            var keyText = key.ToString();

            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Ctrl") : "Ctrl";
                    break;

                case Key.LeftShift:
                case Key.RightShift:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Shift") : "Shift";
                    break;

                case Key.LeftAlt:
                case Key.RightAlt:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Alt") : "Alt";
                    break;
                    
                case Key.LWin:
                case Key.RWin:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Windows") : "Windows";
                    break;

                case Key.Back:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Backspace") : "Backspace";
                    break;

                case Key.Tab:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Tab") : "Tab";
                    break;

                case Key.Return:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Enter") : "Enter";
                    break;

                case Key.Pause:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.PauseBreak") : "Pause/Break";
                    break;

                case Key.CapsLock:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.CapsLock") : "Caps Lock";
                    break;

                case Key.Escape:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Esc") : "Esc";
                    break;

                case Key.Space:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Space") : "Space";
                    break;

                case Key.Prior:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.PageUp") : "Page Up";
                    break;

                case Key.Next:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.PageDown") : "Page Down";
                    break;

                case Key.End:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.End") : "End";
                    break;

                case Key.Home:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Home") : "Home";
                    break;

                case Key.Left:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Left") : "Arrow Left";
                    break;

                case Key.Up:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Up") : "Arrow Up";
                    break;

                case Key.Right:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Right") : "Arrow Right";
                    break;

                case Key.Down:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Down") : "Arrow Down";
                    break;

                case Key.PrintScreen:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.PrintScreen") : "Print Screen";
                    break;

                case Key.Insert:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Insert") : "Insert";
                    break;

                case Key.Delete:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Delete") : "Delete";
                    break;

                case Key.NumLock:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.NumLock") : "Num Lock";
                    break;

                case Key.Scroll:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.ScrollLock") : "Scroll Lock";
                    break;

                case Key.MediaNextTrack:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.NextTrack") : "Next Track";
                    break;

                case Key.MediaPreviousTrack:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.PreviousTrack") : "Previous Track";
                    break;

                case Key.MediaPlayPause:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.PlayPause") : "Play/Pause";
                    break;

                case Key.MediaStop:
                    keyText = translate ? LocalizationHelper.Get("S.Keys.Stop") : "Stop";
                    break;
            }

            //Modifiers;
            return modifiersText + keyText;
        }

        //If there's any modifiers, it means that it's a command. So it should be treated as uppercase.
        if (modifiersText.Length > 0)
            isUppercase = true;

        return modifiersText + (isUppercase ? char.ToUpper(result.Value) : result);
    }

    public static string GetSelectKeyText(ModifierKeys modifier = ModifierKeys.None)
    {
        //Get the modifiers as text.
        var modifiersText = Enum.GetValues(modifier.GetType()).OfType<ModifierKeys>()
            .Where(x => x != ModifierKeys.None && modifier.HasFlag(x))
            .Aggregate("", (current, mod) =>
            {
                if (mod == ModifierKeys.Control) //TODO: Custom mod.ToString();
                    return current + (string.IsNullOrWhiteSpace(current) ? "" : " + ") + "Ctrl";

                return current + (string.IsNullOrWhiteSpace(current) ? "" : " + ") + mod;
            });

        return modifiersText;
    }
}