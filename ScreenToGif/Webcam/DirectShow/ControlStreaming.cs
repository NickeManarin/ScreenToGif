#region License

    /*
        Adapted work from:

        DirectShowLib - Provide access to DirectShow interfaces via .NET
        Copyright (C) 2007
        http://sourceforge.net/projects/directshownet/
        This library is free software; you can redistribute it and/or
        modify it under the terms of the GNU Lesser General Public
        License as published by the Free Software Foundation; either
        version 2.1 of the License, or (at your option) any later version.
        This library is distributed in the hope that it will be useful,
        but WITHOUT ANY WARRANTY; without even the implied warranty of
        MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
        Lesser General Public License for more details.
        You should have received a copy of the GNU Lesser General Public
        License along with this library; if not, write to the Free Software
        Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
    */

#endregion

using System;
using System.Runtime.InteropServices;

namespace ScreenToGif.Webcam.DirectShow;

public class ControlStreaming
{
    /// <summary>
    /// From FILTER_STATE
    /// </summary>
    public enum FilterState
    {
        Stopped,
        Paused,
        Running
    }

    /// <summary>
    /// From WS_* defines
    /// </summary>
    [Flags]
    public enum WindowStyle
    {
        Overlapped = 0x00000000,
        Popup = unchecked((int)0x80000000), // enum can't be uint for VB
        Child = 0x40000000,
        Minimize = 0x20000000,
        Visible = 0x10000000,
        Disabled = 0x08000000,
        ClipSiblings = 0x04000000,
        ClipChildren = 0x02000000,
        Maximize = 0x01000000,
        Caption = 0x00C00000,
        Border = 0x00800000,
        DlgFrame = 0x00400000,
        VScroll = 0x00200000,
        HScroll = 0x00100000,
        SysMenu = 0x00080000,
        ThickFrame = 0x00040000,
        Group = 0x00020000,
        TabStop = 0x00010000,
        MinimizeBox = 0x00020000,
        MaximizeBox = 0x00010000
    }

    /// <summary>
    /// From #define OATRUE/OAFALSE
    /// </summary>
    public enum OABool
    {
        False = 0,
        True = -1 // bools in .NET use 1, not -1
    }

    /// <summary>
    /// From WS_EX_* defines
    /// </summary>
    [Flags]
    public enum WindowStyleEx
    {
        DlgModalFrame = 0x00000001,
        NoParentNotify = 0x00000004,
        Topmost = 0x00000008,
        AcceptFiles = 0x00000010,
        Transparent = 0x00000020,
        MDIChild = 0x00000040,
        ToolWindow = 0x00000080,
        WindowEdge = 0x00000100,
        ClientEdge = 0x00000200,
        ContextHelp = 0x00000400,
        Right = 0x00001000,
        Left = 0x00000000,
        RTLReading = 0x00002000,
        LTRReading = 0x00000000,
        LeftScrollBar = 0x00004000,
        RightScrollBar = 0x00000000,
        ControlParent = 0x00010000,
        StaticEdge = 0x00020000,
        APPWindow = 0x00040000,
        Layered = 0x00080000,
        NoInheritLayout = 0x00100000,
        LayoutRTL = 0x00400000,
        Composited = 0x02000000,
        NoActivate = 0x08000000
    }

    /// <summary>
    /// From SW_* defines
    /// </summary>
    public enum WindowState
    {
        Hide = 0,
        Normal,
        ShowMinimized,
        ShowMaximized,
        ShowNoActivate,
        Show,
        Minimize,
        ShowMinNoActive,
        ShowNA,
        Restore,
        ShowDefault,
        ForceMinimize
    }


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
        int GetState([In] int msTimeout, [Out] out FilterState pfs);

        [PreserveSig]
        int RenderFile([In, MarshalAs(UnmanagedType.BStr)] string strFilename);

        [PreserveSig, Obsolete("Automation interface, for pre-.NET VB.  Use IGraphBuilder::AddSourceFilter instead", false)]
        int AddSourceFilter([In]string strFilename, [Out, MarshalAs(UnmanagedType.IDispatch)] out object ppUnk);

        [PreserveSig, Obsolete("Automation interface, for pre-.NET VB.  Use IFilterGraph::EnumFilters instead", false)]
        int get_FilterCollection([Out, MarshalAs(UnmanagedType.IDispatch)] out object ppUnk);

        [PreserveSig, Obsolete("Automation interface, for pre-.NET VB.  Use IFilterMapper2::EnumMatchingFilters instead", false)]
        int get_RegFilterCollection([Out, MarshalAs(UnmanagedType.IDispatch)] out object ppUnk);

        [PreserveSig]
        int StopWhenReady();
    }

    [ComVisible(true), ComImport, Guid("56a868b4-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IVideoWindow
    {
        [PreserveSig]
        int put_Caption([In, MarshalAs(UnmanagedType.BStr)] string caption);

        [PreserveSig]
        int get_Caption([Out, MarshalAs(UnmanagedType.BStr)] out string caption);

        [PreserveSig]
        int put_WindowStyle([In] WindowStyle windowStyle);

        [PreserveSig]
        int get_WindowStyle([Out] out WindowStyle windowStyle);

        [PreserveSig]
        int put_WindowStyleEx([In] WindowStyleEx windowStyleEx);

        [PreserveSig]
        int get_WindowStyleEx([Out] out WindowStyleEx windowStyleEx);

        [PreserveSig]
        int put_AutoShow([In] OABool autoShow);

        [PreserveSig]
        int get_AutoShow([Out] out OABool autoShow);

        [PreserveSig]
        int put_WindowState([In] WindowState windowState);

        [PreserveSig]
        int get_WindowState([Out] out WindowState windowState);

        [PreserveSig]
        int put_BackgroundPalette([In] OABool backgroundPalette);

        [PreserveSig]
        int get_BackgroundPalette([Out] out OABool backgroundPalette);

        [PreserveSig]
        int put_Visible([In] OABool visible);

        [PreserveSig]
        int get_Visible([Out] out OABool visible);

        [PreserveSig]
        int put_Left([In] int left);

        [PreserveSig]
        int get_Left([Out] out int left);

        [PreserveSig]
        int put_Width([In] int width);

        [PreserveSig]
        int get_Width([Out] out int width);

        [PreserveSig]
        int put_Top([In] int top);

        [PreserveSig]
        int get_Top([Out] out int top);

        [PreserveSig]
        int put_Height([In] int height);

        [PreserveSig]
        int get_Height([Out] out int height);

        [PreserveSig]
        int put_Owner([In] IntPtr owner);

        [PreserveSig]
        int get_Owner([Out] out IntPtr owner);

        [PreserveSig]
        int put_MessageDrain([In] IntPtr drain);

        [PreserveSig]
        int get_MessageDrain([Out] out IntPtr drain);

        // Use ColorTranslator to break out RGB
        [PreserveSig]
        int get_BorderColor([Out] out int color);

        // Use ColorTranslator to break out RGB
        [PreserveSig]
        int put_BorderColor([In] int color);

        [PreserveSig]
        int get_FullScreenMode([Out] out OABool fullScreenMode);

        [PreserveSig]
        int put_FullScreenMode([In] OABool fullScreenMode);

        [PreserveSig]
        int SetWindowForeground([In] OABool focus);

        [PreserveSig]
        int NotifyOwnerMessage([In] IntPtr hwnd, [In] int msg, [In] IntPtr wParam, [In] IntPtr lParam);

        [PreserveSig]
        int SetWindowPosition([In] int left, [In] int top, [In] int width, [In] int height);

        [PreserveSig]
        int GetWindowPosition([Out] out int left, [Out] out int top, [Out] out int width, [Out] out int height);

        [PreserveSig]
        int GetMinIdealImageSize([Out] out int width, [Out] out int height);

        [PreserveSig]
        int GetMaxIdealImageSize([Out] out int width, [Out] out int height);

        [PreserveSig]
        int GetRestorePosition([Out] out int left, [Out] out int top, [Out] out int width, [Out] out int height);

        [PreserveSig]
        int HideCursor([In] OABool hideCursor);

        [PreserveSig]
        int IsCursorHidden([Out] out OABool hideCursor);
    }
}