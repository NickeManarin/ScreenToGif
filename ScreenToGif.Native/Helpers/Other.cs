using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Domain.Models.Native;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;
using Point = System.Windows.Point;

namespace ScreenToGif.Native.Helpers;

public class Other
{
    /// <summary>
    /// Draws a rectangle over a Window.
    /// </summary>
    /// <param name="hWnd">The window handle.</param>
    /// <param name="scale">Window scale.</param>
    public static void DrawFrame(IntPtr hWnd, double scale)
    {
        //TODO: Adjust for high DPI.
        if (hWnd == IntPtr.Zero)
            return;

        var hdc = User32.GetWindowDC(hWnd); //GetWindowDC((IntPtr) null);

        User32.GetWindowRect(hWnd, out NativeRect rect);

        //DwmGetWindowAttribute(hWnd, (int)DwmWindowAttribute.DwmwaExtendedFrameBounds, out rect, Marshal.SizeOf(typeof(Rect)));
        User32.OffsetRect(ref rect, -rect.Left, -rect.Top);

        const int frameWidth = 3;

        Gdi32.PatBlt(hdc, rect.Left, rect.Top, rect.Right - rect.Left, frameWidth, Constants.DstInvert);

        Gdi32.PatBlt(hdc, rect.Left, rect.Bottom - frameWidth, frameWidth, -(rect.Bottom - rect.Top - 2 * frameWidth), Constants.DstInvert);

        Gdi32.PatBlt(hdc, rect.Right - frameWidth, rect.Top + frameWidth, frameWidth, rect.Bottom - rect.Top - 2 * frameWidth, Constants.DstInvert);

        Gdi32.PatBlt(hdc, rect.Right, rect.Bottom - frameWidth, -(rect.Right - rect.Left), frameWidth, Constants.DstInvert);
    }

    internal static string CheckAllFlags(uint style, bool isExtended)
    {
        var values = Enum.GetValues(typeof(WindowStyles)).OfType<WindowStyles>().Distinct().ToList();
        var valuesEx = Enum.GetValues(typeof(WindowStylesEx)).OfType<WindowStylesEx>().Distinct().ToList();

        var text = "";

        if (!isExtended)
            text = values.Where(value => (style & (uint)value) == (uint)value).Aggregate(text, (current, value) => current + (value + ", "));
        else
            text = valuesEx.Where(value => (style & (uint)value) == (uint)value).Aggregate(text, (current, value) => current + (value + ", "));

        return text.TrimEnd(' ').TrimEnd(',');
    }

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

    public static string GetSelectKeyText(Key key, ModifierKeys modifier = ModifierKeys.None, bool isUppercase = false, bool ignoreNone = false)
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
                    keyText = "Ctrl";
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    keyText = "Shift";
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    keyText = "Alt";
                    break;
                case Key.CapsLock:
                    keyText = "CapsLock";
                    break;
                case Key.LWin:
                case Key.RWin:
                    keyText = "Windows";
                    break;
                case Key.Return:
                    keyText = "Enter";
                    break;
                case Key.Next:
                    keyText = "PageDown";
                    break;
                case Key.PrintScreen:
                    keyText = "PrintScreen";
                    break;
                case Key.Back:
                    keyText = "Backspace";
                    break;

                //Special localization
                case Key.Space:
                    keyText = LocalizationHelper.Get("S.Keys.Space");
                    break;
            }

            //Modifiers;
            return modifiersText + keyText;

            #region Try it later

            /*
                Declare Function ToAscii Lib "user32" (ByVal uVirtKey As Integer, ByVal uScanCode As Integer, ByRef lpbKeyState As Byte, ByRef lpwTransKey As Integer, ByVal fuState As Integer) As Integer
                Declare Function GetKeyboardState Lib "user32.dll" (ByRef pbKeyState As Byte) As Long

                Private Function GetCharFromKey(ByVal KeyCode As Integer) As String
                    Dim KeyBoardState(255) As Byte
                    Dim Out As Long
                    
                    If GetKeyboardState(KeyBoardState(0)) <0 Then
                        If ToAscii(KeyCode, 0, KeyBoardState(0), Out, 0) <0 Then
                            If Out <= 255 Then
                                GetCharFromKey = Chr(Out)
                            Else
                                'GetCharFromKey = Microsoft.VisualBasic.Left(StrConv(ChrW(Out), vbUnicode), 1)
                                GetCharFromKey = Microsoft.VisualBasic.Left(StrConv(ChrW(Out), VbStrConv.None), 1)
                            End If
                        Else
                            GetCharFromKey = ""
                        End If
                    Else
                        GetCharFromKey = ""
                    End If
                End Function 
                
            */

            #endregion
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

    public static Point GetMousePosition(double scale = 1, double offsetX = 0, double offsetY = 0)
    {
        var point = new PointW();
        User32.GetCursorPos(ref point);
        return new Point(point.X / scale - offsetX, point.Y / scale - offsetY);
    }

    public static bool ShowFileProperties(string filename)
    {
        var info = new ShellExecuteInfo();
        info.cbSize = Marshal.SizeOf(info);
        info.lpVerb = "properties";
        info.lpFile = filename;
        //info.lpParameters = "Security";
        info.nShow = (int)ShowWindowCommands.Show;
        info.fMask = (uint)ShellExecuteMasks.InvokeIdList;
        return Shell32.ShellExecuteEx(ref info);
    }

    private static IntPtr StructToPtr(object obj)
    {
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
        Marshal.StructureToPtr(obj, ptr, false);
        return ptr;
    }
}