using System;
using System.Runtime.InteropServices;

namespace ScreenToGif.Webcam.DirectShow
{
    public class ControlStreaming
    {
        [ComVisible(true), ComImport, Guid("56a868b1-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
        public interface IMediaControl
        {
            [PreserveSig]
            int Run();

            [PreserveSig]
            int Pause();

            [PreserveSig]
            int Stop();

            [PreserveSig]
            int GetState(int msTimeout, out int pfs);

            [PreserveSig]
            int RenderFile(string strFilename);

            [PreserveSig]
            int AddSourceFilter(
                [In]											string strFilename,
                [Out, MarshalAs(UnmanagedType.IDispatch)]	out object ppUnk);

            [PreserveSig]
            int get_FilterCollection(
                [Out, MarshalAs(UnmanagedType.IDispatch)]	out object ppUnk);

            [PreserveSig]
            int get_RegFilterCollection(
                [Out, MarshalAs(UnmanagedType.IDispatch)]	out object ppUnk);

            [PreserveSig]
            int StopWhenReady();
        }

        [ComVisible(true), ComImport, Guid("56a868b4-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
        public interface IVideoWindow
        {
            [PreserveSig]
            int put_Caption(string caption);
            [PreserveSig]
            int get_Caption([Out] out string caption);

            [PreserveSig]
            int put_WindowStyle(int windowStyle);
            [PreserveSig]
            int get_WindowStyle(out int windowStyle);

            [PreserveSig]
            int put_WindowStyleEx(int windowStyleEx);
            [PreserveSig]
            int get_WindowStyleEx(out int windowStyleEx);

            [PreserveSig]
            int put_AutoShow(int autoShow);
            [PreserveSig]
            int get_AutoShow(out int autoShow);

            [PreserveSig]
            int put_WindowState(int windowState);
            [PreserveSig]
            int get_WindowState(out int windowState);

            [PreserveSig]
            int put_BackgroundPalette(int backgroundPalette);
            [PreserveSig]
            int get_BackgroundPalette(out int backgroundPalette);

            [PreserveSig]
            int put_Visible(int visible);
            [PreserveSig]
            int get_Visible(out int visible);

            [PreserveSig]
            int put_Left(int left);
            [PreserveSig]
            int get_Left(out int left);

            [PreserveSig]
            int put_Width(int width);
            [PreserveSig]
            int get_Width(out int width);

            [PreserveSig]
            int put_Top(int top);
            [PreserveSig]
            int get_Top(out int top);

            [PreserveSig]
            int put_Height(int height);
            [PreserveSig]
            int get_Height(out int height);

            [PreserveSig]
            int put_Owner(IntPtr owner);
            [PreserveSig]
            int get_Owner(out IntPtr owner);

            [PreserveSig]
            int put_MessageDrain(IntPtr drain);
            [PreserveSig]
            int get_MessageDrain(out IntPtr drain);

            [PreserveSig]
            int get_BorderColor(out int color);
            [PreserveSig]
            int put_BorderColor(int color);

            [PreserveSig]
            int get_FullScreenMode(out int fullScreenMode);
            [PreserveSig]
            int put_FullScreenMode(int fullScreenMode);

            [PreserveSig]
            int SetWindowForeground(int focus);

            [PreserveSig]
            int NotifyOwnerMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

            [PreserveSig]
            int SetWindowPosition(int left, int top, int width, int height);

            [PreserveSig]
            int GetWindowPosition(out int left, out int top, out int width, out int height);

            [PreserveSig]
            int GetMinIdealImageSize(out int width, out int height);

            [PreserveSig]
            int GetMaxIdealImageSize(out int width, out int height);

            [PreserveSig]
            int GetRestorePosition(out int left, out int top, out int width, out int height);

            [PreserveSig]
            int HideCursor(int hideCursor);

            [PreserveSig]
            int IsCursorHidden(out int hideCursor);

        }
    }
}
