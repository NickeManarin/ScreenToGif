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
}