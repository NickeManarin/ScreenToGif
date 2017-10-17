using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ScreenToGif.FileWriters;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace ScreenToGif.Util
{
    public static class Native
    {
        #region Variables/Const

        internal static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        internal const int MonitorDefaultToNull = 0;
        internal const int MonitorDefaultToPrimary = 1;
        internal const int MonitorDefaultToNearest = 2;

        internal const int CursorShowing = 0x00000001;
        internal const int DstInvert = 0x00550009;

        internal const int DiNormal = 0x0003;

        internal const int MonitorinfofPrimary = 0x00000001;

        internal const int StateSystemUnavailable = 0x0001;
        internal const int StateSystemInvisible = 0x8000;
        internal const int StateSystemOffscreen = 0x010000;

        #region Window consts

        internal enum WindowStyles : uint
        {
            Overlapped = 0,
            Popup = 0x80000000,
            Child = 0x40000000,
            Minimize = 0x20000000,
            Visible = 0x10000000,
            Disabled = 0x8000000,
            Clipsiblings = 0x4000000,
            Clipchildren = 0x2000000,
            Maximize = 0x1000000,
            Caption = 0xC00000, //WS_BORDER or WS_DLGFRAME  
            Border = 0x800000,
            Dlgframe = 0x400000,
            Vscroll = 0x200000,
            Hscroll = 0x100000,
            Sysmenu = 0x80000,
            Thickframe = 0x40000,
            Group = 0x20000,
            Tabstop = 0x10000,
            Minimizebox = 0x20000,
            Maximizebox = 0x10000,
            Tiled = WindowStyles.Overlapped,
            Iconic = WindowStyles.Minimize,
            Sizebox = WindowStyles.Thickframe,
        }

        internal enum WindowStylesEx : uint
        {
            //Extended Window Styles.
            Dlgmodalframe = 0x0001,
            Noparentnotify = 0x0004,
            Topmost = 0x0008,
            Acceptfiles = 0x0010,
            Transparent = 0x0020,
            Mdichild = 0x0040,
            Toolwindow = 0x0080,
            Windowedge = 0x0100,
            Clientedge = 0x0200,
            Contexthelp = 0x0400,
            Right = 0x1000,
            Left = 0x0000,
            Rtlreading = 0x2000,
            Ltrreading = 0x0000,
            Leftscrollbar = 0x4000,
            Rightscrollbar = 0x0000,
            Controlparent = 0x10000,
            Staticedge = 0x20000,
            Appwindow = 0x40000,
            Overlappedwindow = (Windowedge | Clientedge),
            Palettewindow = (Windowedge | Toolwindow | Topmost),
            Layered = 0x00080000,
            Noinheritlayout = 0x00100000, // Disable inheritence of mirroring by children
            Layoutrtl = 0x00400000, // Right to left mirroring
            Composited = 0x02000000,
            Noactivate = 0x08000000,
        }

        #endregion

        [StructLayout(LayoutKind.Sequential)]
        internal struct Iconinfo
        {
            public bool fIcon;      // Specifies whether this structure defines an icon or a cursor. A value of TRUE specifies 
            public int xHotspot;    // Specifies the x-coordinate of a cursor's hot spot. If this structure defines an icon, the hot 
            public int yHotspot;    // Specifies the y-coordinate of the cursor's hot spot. If this structure defines an icon, the hot 
            public IntPtr hbmMask;  // (HBITMAP) Specifies the icon bitmask bitmap. If this structure defines a black and white icon, 
            public IntPtr hbmColor; // (HBITMAP) Handle to the icon color bitmap. This member can be optional if this 
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PointW
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CursorInfo
        {
            /// <summary>
            /// Specifies the size, in bytes, of the structure. 
            /// </summary>
            public int cbSize;

            /// <summary>
            /// Specifies the cursor state. This parameter can be one of the following values:
            /// </summary>
            public int flags;

            ///<summary>
            ///Handle to the cursor. 
            ///</summary>
            public IntPtr hCursor;

            /// <summary>
            /// A POINT structure that receives the screen coordinates of the cursor. 
            /// </summary>
            public PointW ptScreenPos;
        }

        ///<summary>
        ///Specifies a raster-operation code. These codes define how the color data for the
        ///source rectangle is to be combined with the color data for the destination
        ///rectangle to achieve the final color.
        ///</summary>
        internal enum CopyPixelOperation
        {
            NoMirrorBitmap = -2147483648,

            /// <summary>dest = BLACK, 0x00000042</summary>
            Blackness = 66,

            ///<summary>dest = (NOT src) AND (NOT dest), 0x001100A6</summary>
            NotSourceErase = 1114278,

            ///<summary>dest = (NOT source), 0x00330008</summary>
            NotSourceCopy = 3342344,

            ///<summary>dest = source AND (NOT dest), 0x00440328</summary>
            SourceErase = 4457256,

            /// <summary>dest = (NOT dest), 0x00550009</summary>
            DestinationInvert = 5570569,

            /// <summary>dest = pattern XOR dest, 0x005A0049</summary>
            PatInvert = 5898313,

            ///<summary>dest = source XOR dest, 0x00660046</summary>
            SourceInvert = 6684742,

            ///<summary>dest = source AND dest, 0x008800C6</summary>
            SourceAnd = 8913094,

            /// <summary>dest = (NOT source) OR dest, 0x00BB0226</summary>
            MergePaint = 12255782,

            ///<summary>dest = (source AND pattern), 0x00C000CA</summary>
            MergeCopy = 12583114,

            ///<summary>dest = source, 0x00CC0020</summary>
            SourceCopy = 13369376,

            /// <summary>dest = source OR dest, 0x00EE0086</summary>
            SourcePaint = 15597702,

            /// <summary>dest = pattern, 0x00F00021</summary>
            PatCopy = 15728673,

            /// <summary>dest = DPSnoo, 0x00FB0A09</summary>
            PatPaint = 16452105,

            /// <summary>dest = WHITE, 0x00FF0062</summary>
            Whiteness = 16711778,

            /// <summary>
            /// Capture window as seen on screen.  This includes layered windows 
            /// such as WPF windows with AllowsTransparency="true", 0x40000000
            /// </summary>
            CaptureBlt = 1073741824,
        }

        internal enum DeviceCaps : int
        {
            LogPixelsX = 88,
            LogPixelsY = 90,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner

            public Int32Rect ToRectangle()
            {
                return new Int32Rect(Left, Top, Right - Left, Bottom - Top);
            }

            public System.Windows.Rect ToRect(double offset = 0, double scale = 1d)
            {
                return new System.Windows.Rect((Left - offset) / scale, (Top - offset) / scale, (Right - Left + offset * 2) / scale, (Bottom - Top + offset * 2) / scale);
            }
        }

        internal struct Margins
        {
            public Margins(Thickness t)
            {
                Left = (int)t.Left;
                Right = (int)t.Right;
                Top = (int)t.Top;
                Bottom = (int)t.Bottom;
            }

            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        internal enum ProcessDpiAwareness
        {
            ProcessDpiUnaware = 0,
            ProcessSystemDpiAware = 1,
            ProcessPerMonitorDpiAware = 2
        }

        internal enum DwmWindowAttribute
        {
            NcRenderingEnabled = 1,
            NcRenderingPolicy,
            TransitionsForcedisabled,
            AllowNcPaint,
            CaptionButtonBounds,
            NonclientRtlLayout,
            ForceIconicRepresentation,
            Flip3DPolicy,
            ExtendedFrameBounds,
            HasIconicBitmap,
            DisallowPeek,
            ExcludedFromPeek,
            Cloak,
            Cloaked,
            FreezeRepresentation,
            Last
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        internal class MonitorInfoEx
        {
            public int cbSize = Marshal.SizeOf(typeof(MonitorInfoEx));
            public Rect rcMonitor = new Rect();
            public Rect rcWork = new Rect();
            public int dwFlags = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szDevice = new char[32];
        }

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
        /// </summary>
        public enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2
        }

        [Flags]
        private enum KnownFolderFlags : uint
        {
            SimpleIDList = 0x00000100,
            NotParentRelative = 0x00000200,
            DefaultPath = 0x00000400,
            Init = 0x00000800,
            NoAlias = 0x00001000,
            DontUnexpand = 0x00002000,
            DontVerify = 0x00004000,
            Create = 0x00008000,
            NoAppcontainerRedirection = 0x00010000,
            AliasOnly = 0x80000000
        }

        internal static class KnownFolder
        {
            internal static readonly Guid AddNewPrograms = new Guid("de61d971-5ebc-4f02-a3a9-6c82895e5c04");
            internal static readonly Guid AdminTools = new Guid("724EF170-A42D-4FEF-9F26-B60E846FBA4F");
            internal static readonly Guid AppUpdates = new Guid("a305ce99-f527-492b-8b1a-7e76fa98d6e4");
            internal static readonly Guid CDBurning = new Guid("9E52AB10-F80D-49DF-ACB8-4330F5687855");
            internal static readonly Guid ChangeRemovePrograms = new Guid("df7266ac-9274-4867-8d55-3bd661de872d");
            internal static readonly Guid CommonAdminTools = new Guid("D0384E7D-BAC3-4797-8F14-CBA229B392B5");
            internal static readonly Guid CommonOEMLinks = new Guid("C1BAE2D0-10DF-4334-BEDD-7AA20B227A9D");
            internal static readonly Guid CommonPrograms = new Guid("0139D44E-6AFE-49F2-8690-3DAFCAE6FFB8");
            internal static readonly Guid CommonStartMenu = new Guid("A4115719-D62E-491D-AA7C-E74B8BE3B067");
            internal static readonly Guid CommonStartup = new Guid("82A5EA35-D9CD-47C5-9629-E15D2F714E6E");
            internal static readonly Guid CommonTemplates = new Guid("B94237E7-57AC-4347-9151-B08C6C32D1F7");
            internal static readonly Guid ComputerFolder = new Guid("0AC0837C-BBF8-452A-850D-79D08E667CA7");
            internal static readonly Guid ConflictFolder = new Guid("4bfefb45-347d-4006-a5be-ac0cb0567192");
            internal static readonly Guid ConnectionsFolder = new Guid("6F0CD92B-2E97-45D1-88FF-B0D186B8DEDD");
            internal static readonly Guid Contacts = new Guid("56784854-C6CB-462b-8169-88E350ACB882");
            internal static readonly Guid ControlPanelFolder = new Guid("82A74AEB-AEB4-465C-A014-D097EE346D63");
            internal static readonly Guid Cookies = new Guid("2B0F765D-C0E9-4171-908E-08A611B84FF6");
            internal static readonly Guid Desktop = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
            internal static readonly Guid Documents = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
            internal static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            internal static readonly Guid Favorites = new Guid("1777F761-68AD-4D8A-87BD-30B759FA33DD");
            internal static readonly Guid Fonts = new Guid("FD228CB7-AE11-4AE3-864C-16F3910AB8FE");
            internal static readonly Guid Games = new Guid("CAC52C1A-B53D-4edc-92D7-6B2E8AC19434");
            internal static readonly Guid GameTasks = new Guid("054FAE61-4DD8-4787-80B6-090220C4B700");
            internal static readonly Guid History = new Guid("D9DC8A3B-B784-432E-A781-5A1130A75963");
            internal static readonly Guid InternetCache = new Guid("352481E8-33BE-4251-BA85-6007CAEDCF9D");
            internal static readonly Guid InternetFolder = new Guid("4D9F7874-4E0C-4904-967B-40B0D20C3E4B");
            internal static readonly Guid Links = new Guid("bfb9d5e0-c6a9-404c-b2b2-ae6db6af4968");
            internal static readonly Guid LocalAppData = new Guid("F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
            internal static readonly Guid LocalAppDataLow = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
            internal static readonly Guid LocalizedResourcesDir = new Guid("2A00375E-224C-49DE-B8D1-440DF7EF3DDC");
            internal static readonly Guid Music = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43");
            internal static readonly Guid NetHood = new Guid("C5ABBF53-E17F-4121-8900-86626FC2C973");
            internal static readonly Guid NetworkFolder = new Guid("D20BEEC4-5CA8-4905-AE3B-BF251EA09B53");
            internal static readonly Guid OriginalImages = new Guid("2C36C0AA-5812-4b87-BFD0-4CD0DFB19B39");
            internal static readonly Guid PhotoAlbums = new Guid("69D2CF90-FC33-4FB7-9A0C-EBB0F0FCB43C");
            internal static readonly Guid Pictures = new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB");
            internal static readonly Guid Playlists = new Guid("DE92C1C7-837F-4F69-A3BB-86E631204A23");
            internal static readonly Guid PrintersFolder = new Guid("76FC4E2D-D6AD-4519-A663-37BD56068185");
            internal static readonly Guid PrintHood = new Guid("9274BD8D-CFD1-41C3-B35E-B13F55A758F4");
            internal static readonly Guid Profile = new Guid("5E6C858F-0E22-4760-9AFE-EA3317B67173");
            internal static readonly Guid ProgramData = new Guid("62AB5D82-FDC1-4DC3-A9DD-070D1D495D97");
            internal static readonly Guid ProgramFiles = new Guid("905e63b6-c1bf-494e-b29c-65b732d3d21a");
            internal static readonly Guid ProgramFilesX64 = new Guid("6D809377-6AF0-444b-8957-A3773F02200E");
            internal static readonly Guid ProgramFilesX86 = new Guid("7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E");
            internal static readonly Guid ProgramFilesCommon = new Guid("F7F1ED05-9F6D-47A2-AAAE-29D317C6F066");
            internal static readonly Guid ProgramFilesCommonX64 = new Guid("6365D5A7-0F0D-45E5-87F6-0DA56B6A4F7D");
            internal static readonly Guid ProgramFilesCommonX86 = new Guid("DE974D24-D9C6-4D3E-BF91-F4455120B917");
            internal static readonly Guid Programs = new Guid("A77F5D77-2E2B-44C3-A6A2-ABA601054A51");
            internal static readonly Guid Public = new Guid("DFDF76A2-C82A-4D63-906A-5644AC457385");
            internal static readonly Guid PublicDesktop = new Guid("C4AA340D-F20F-4863-AFEF-F87EF2E6BA25");
            internal static readonly Guid PublicDocuments = new Guid("ED4824AF-DCE4-45A8-81E2-FC7965083634");
            internal static readonly Guid PublicDownloads = new Guid("3D644C9B-1FB8-4f30-9B45-F670235F79C0");
            internal static readonly Guid PublicGameTasks = new Guid("DEBF2536-E1A8-4c59-B6A2-414586476AEA");
            internal static readonly Guid PublicMusic = new Guid("3214FAB5-9757-4298-BB61-92A9DEAA44FF");
            internal static readonly Guid PublicPictures = new Guid("B6EBFB86-6907-413C-9AF7-4FC2ABF07CC5");
            internal static readonly Guid PublicVideos = new Guid("2400183A-6185-49FB-A2D8-4A392A602BA3");
            internal static readonly Guid QuickLaunch = new Guid("52a4f021-7b75-48a9-9f6b-4b87a210bc8f");
            internal static readonly Guid Recent = new Guid("AE50C081-EBD2-438A-8655-8A092E34987A");
            internal static readonly Guid RecycleBinFolder = new Guid("B7534046-3ECB-4C18-BE4E-64CD4CB7D6AC");
            internal static readonly Guid ResourceDir = new Guid("8AD10C31-2ADB-4296-A8F7-E4701232C972");
            internal static readonly Guid RoamingAppData = new Guid("3EB685DB-65F9-4CF6-A03A-E3EF65729F3D");
            internal static readonly Guid SampleMusic = new Guid("B250C668-F57D-4EE1-A63C-290EE7D1AA1F");
            internal static readonly Guid SamplePictures = new Guid("C4900540-2379-4C75-844B-64E6FAF8716B");
            internal static readonly Guid SamplePlaylists = new Guid("15CA69B3-30EE-49C1-ACE1-6B5EC372AFB5");
            internal static readonly Guid SampleVideos = new Guid("859EAD94-2E85-48AD-A71A-0969CB56A6CD");
            internal static readonly Guid SavedGames = new Guid("4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4");
            internal static readonly Guid SavedSearches = new Guid("7d1d3a04-debb-4115-95cf-2f29da2920da");
            internal static readonly Guid SEARCH_CSC = new Guid("ee32e446-31ca-4aba-814f-a5ebd2fd6d5e");
            internal static readonly Guid SEARCH_MAPI = new Guid("98ec0e18-2098-4d44-8644-66979315a281");
            internal static readonly Guid SearchHome = new Guid("190337d1-b8ca-4121-a639-6d472d16972a");
            internal static readonly Guid SendTo = new Guid("8983036C-27C0-404B-8F08-102D10DCFD74");
            internal static readonly Guid SidebarDefaultParts = new Guid("7B396E54-9EC5-4300-BE0A-2482EBAE1A26");
            internal static readonly Guid SidebarParts = new Guid("A75D362E-50FC-4fb7-AC2C-A8BEAA314493");
            internal static readonly Guid StartMenu = new Guid("625B53C3-AB48-4EC1-BA1F-A1EF4146FC19");
            internal static readonly Guid Startup = new Guid("B97D20BB-F46A-4C97-BA10-5E3608430854");
            internal static readonly Guid SyncManagerFolder = new Guid("43668BF8-C14E-49B2-97C9-747784D784B7");
            internal static readonly Guid SyncResultsFolder = new Guid("289a9a43-be44-4057-a41b-587a76d7e7f9");
            internal static readonly Guid SyncSetupFolder = new Guid("0F214138-B1D3-4a90-BBA9-27CBC0C5389A");
            internal static readonly Guid System = new Guid("1AC14E77-02E7-4E5D-B744-2EB1AE5198B7");
            internal static readonly Guid SystemX86 = new Guid("D65231B0-B2F1-4857-A4CE-A8E7C6EA7D27");
            internal static readonly Guid Templates = new Guid("A63293E8-664E-48DB-A079-DF759E0509F7");
            internal static readonly Guid TreeProperties = new Guid("5b3749ad-b49f-49c1-83eb-15370fbd4882");
            internal static readonly Guid UserProfiles = new Guid("0762D272-C50A-4BB0-A382-697DCD729B80");
            internal static readonly Guid UsersFiles = new Guid("f3ce0f7c-4901-4acc-8648-d5d44b04ef8f");
            internal static readonly Guid Videos = new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC");
            internal static readonly Guid Windows = new Guid("F38BF404-1D43-42F2-9305-67DE0B28FC23");
        }

        public enum MapType : uint
        {
            MapvkVkToVsc = 0x0,
            MapvkVscToVk = 0x1,
            MapvkVkToChar = 0x2,
            MapvkVscToVkEx = 0x3,
        }

        private enum Gwl
        {
            GwlWndproc = -4,
            GwlHinstance = -6,
            GwlHwndparent = -8,
            GwlStyle = -16,
            GwlExstyle = -20,
            GwlUserdata = -21,
            GwlId = -12
        }

        private enum GetAncestorFlags
        {
            /// <summary>
            /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function. 
            /// </summary>
            GetParent = 1,
            /// <summary>
            /// Retrieves the root window by walking the chain of parent windows.
            /// </summary>
            GetRoot = 2,
            /// <summary>
            /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent. 
            /// </summary>
            GetRootOwner = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowInfo
        {
            internal uint cbSize;
            internal Rect rcWindow;
            internal Rect rcClient;
            internal uint dwStyle;
            internal uint dwExStyle;
            internal uint dwWindowStatus;
            internal uint cxWindowBorders;
            internal uint cyWindowBorders;
            internal ushort atomWindowType;
            internal ushort wCreatorVersion;

            internal WindowInfo(bool? filler) : this()
            {
                //Allows automatic initialization of "cbSize" with "new WindowInfo(null/true/false)".
                cbSize = (uint)Marshal.SizeOf(typeof(WindowInfo));
            }
        }

        internal const int CChildrenTitlebar = 5;

        [StructLayout(LayoutKind.Sequential)]
        internal struct TitlebarInfo
        {
            internal int cbSize;
            internal Rect rcTitleBar;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CChildrenTitlebar + 1)]
            internal int[] rgstate;

            internal TitlebarInfo(bool? filler) : this()
            {
                //Allows automatic initialization of "cbSize" with "new TitlebarInfo(null/true/false)".
                cbSize = (int)Marshal.SizeOf(typeof(TitlebarInfo));
            }
        }

        private enum GetWindowType : uint
        {
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is highest in the Z order.
            /// <para/>
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            HwndFirst = 0,
            
            /// <summary>
            /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            HwdnLast = 1,
            
            /// <summary>
            /// The retrieved handle identifies the window below the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            HwndNext = 2,
            
            /// <summary>
            /// The retrieved handle identifies the window above the specified window in the Z order.
            /// <para />
            /// If the specified window is a topmost window, the handle identifies a topmost window.
            /// If the specified window is a top-level window, the handle identifies a top-level window.
            /// If the specified window is a child window, the handle identifies a sibling window.
            /// </summary>
            HwndPrev = 3,
            
            /// <summary>
            /// The retrieved handle identifies the specified window's owner window, if any.
            /// </summary>
            Owner = 4,
            
            /// <summary>
            /// The retrieved handle identifies the child window at the top of the Z order,
            /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
            /// The function examines only child windows of the specified window. It does not examine descendant windows.
            /// </summary>
            Child = 5,

            /// <summary>
            /// The retrieved handle identifies the enabled popup window owned by the specified window (the
            /// search uses the first such window found using HwndNext); otherwise, if there are no enabled
            /// popup windows, the retrieved handle is that of the specified window.
            /// </summary>
            EnabledPopup = 6
        }

        internal struct MemoryStatusEx
        {
            internal uint Length;
            internal uint MemoryLoad;
            internal ulong TotalPhysicalMemory;
            internal ulong AvailablePhysicalMemory;
            internal ulong TotalPageFile;
            internal ulong AvailablePageFile;
            internal ulong TotalVirtualMemory;
            internal ulong AvailableVirtualMemory;
            internal ulong AvailableExtendedVirtual;

            internal MemoryStatusEx(bool? filler) : this()
            {
                Length = checked((uint)Marshal.SizeOf(typeof(MemoryStatusEx)));
            }
        }

        #endregion

        #region Functions

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref PointW lpPoint);

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref PointW lpPoint);

        [DllImport("user32.dll", EntryPoint = "GetCursorInfo")]
        internal static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll", EntryPoint = "CopyIcon")]
        internal static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", EntryPoint = "GetIconInfo")]
        internal static extern bool GetIconInfo(IntPtr hIcon, out Iconinfo piconinfo);

        ///<summary>
        ///Creates a memory device context (DC) compatible with the specified device.
        ///</summary>
        ///<param name="hdc">A handle to an existing DC. If this handle is NULL,
        ///the function creates a memory DC compatible with the application's current screen.</param>
        ///<returns>
        ///If the function succeeds, the return value is the handle to a memory DC.
        ///If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.
        ///</returns>
        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        internal static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        ///<summary>Selects an object into the specified device context (DC). The new object replaces the previous object of the same type.</summary>
        ///<param name="hdc">A handle to the DC.</param>
        ///<param name="hgdiobj">A handle to the object to be selected.</param>
        ///<returns>
        ///<para>If the selected object is not a region and the function succeeds, the return value is a handle to the object being replaced. If the selected object is a region and the function succeeds, the return value is one of the following values.</para>
        ///<para>SIMPLEREGION - Region consists of a single rectangle.</para>
        ///<para>COMPLEXREGION - Region consists of more than one rectangle.</para>
        ///<para>NULLREGION - Region is empty.</para>
        ///<para>If an error occurs and the selected object is not a region, the return value is <c>NULL</c>. Otherwise, it is <c>HGDI_ERROR</c>.</para>
        ///</returns>
        ///<remarks>
        ///<para>This function returns the previously selected object of the specified type. An application should always replace a new object with the original, default object after it has finished drawing with the new object.</para>
        ///<para>An application cannot select a single bitmap into more than one DC at a time.</para>
        ///<para>ICM: If the object being selected is a brush or a pen, color management is performed.</para>
        ///</remarks>
        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        internal static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        ///<summary>
        ///Performs a bit-block transfer of the color data corresponding to a
        ///rectangle of pixels from the specified source device context into
        ///a destination device context.
        ///</summary>
        ///<param name="hdc">Handle to the destination device context.</param>
        ///<param name="nXDest">The leftmost x-coordinate of the destination rectangle (in pixels).</param>
        ///<param name="nYDest">The topmost y-coordinate of the destination rectangle (in pixels).</param>
        ///<param name="nWidth">The width of the source and destination rectangles (in pixels).</param>
        ///<param name="nHeight">The height of the source and the destination rectangles (in pixels).</param>
        ///<param name="hdcSrc">Handle to the source device context.</param>
        ///<param name="nXSrc">The leftmost x-coordinate of the source rectangle (in pixels).</param>
        ///<param name="nYSrc">The topmost y-coordinate of the source rectangle (in pixels).</param>
        ///<param name="dwRop">A raster-operation code.</param>
        ///<returns>
        ///<c>true</c> if the operation succeedes, <c>false</c> otherwise. To get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
        ///</returns>
        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        ///<summary>Deletes the specified device context (DC).</summary>
        ///<param name="hdc">A handle to the device context.</param>
        ///<returns><para>If the function succeeds, the return value is nonzero.</para><para>If the function fails, the return value is zero.</para></returns>
        ///<remarks>An application must not delete a DC whose handle was obtained by calling the <c>GetDC</c> function. Instead, it must call the <c>ReleaseDC</c> function to free the DC.</remarks>
        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        internal static extern bool DeleteDC([In] IntPtr hdc);

        ///<summary>Deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object. After the object is deleted, the specified handle is no longer valid.</summary>
        ///<param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
        ///<returns>
        ///<para>If the function succeeds, the return value is nonzero.</para>
        ///<para>If the specified handle is not valid or is currently selected into a DC, the return value is zero.</para>
        ///</returns>
        ///<remarks>
        ///<para>Do not delete a drawing object (pen or brush) while it is still selected into a DC.</para>
        ///<para>When a pattern brush is deleted, the bitmap associated with the brush is not deleted. The bitmap must be deleted independently.</para>
        ///</remarks>
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject([In] IntPtr hObject);

        [DllImport("user32.dll", SetLastError = false)]
        internal static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowDC(IntPtr ptr);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        /// <summary>
        /// Releases the device context from the given window handle.
        /// </summary>
        /// <param name="hWnd">The window handle</param>
        /// <param name="hDc">The device context handle.</param>
        /// <returns>True if successful</returns>
        [DllImport("user32.dll")]
        internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll")]
        internal static extern int GetDeviceCaps(IntPtr hdc, int capindex);

        [DllImport("user32.dll")]
        internal static extern IntPtr WindowFromPoint(PointW point);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out bool pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

        [DllImport("kernel32.dll")]
        internal static extern int GetProcessId(IntPtr handle);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        internal static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        internal static extern bool DwmIsCompositionEnabled();

        [DllImport("gdi32.dll")]
        internal static extern bool PatBlt(IntPtr hdc, int nXLeft, int nYLeft, int nWidth, int nHeight, uint dwRop);

        [DllImport("user32.dll")]
        internal static extern bool OffsetRect(ref Rect lprc, int dx, int dy);

        [DllImport("gdi32.dll")]
        internal static extern bool GetCurrentPositionEx(IntPtr hdc, out PointW lpPoint);

        [DllImport("gdi32.dll")]
        internal static extern bool GetWindowOrgEx(IntPtr hdc, out PointW lpPoint);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr MonitorFromPoint(PointW pt, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out]MonitorInfoEx info);

        [DllImport("user32.dll", ExactSpelling = true)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern bool EnumDisplayMonitors(HandleRef hdc, IntPtr rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        /// </summary>
        /// <param name="hmonitor">Handle of the monitor being queried.</param>
        /// <param name="dpiType">The type of DPI being queried. Possible values are from the MONITOR_DPI_TYPE enumeration.</param>
        /// <param name="dpiX">The value of the DPI along the X axis. This value always refers to the horizontal edge, even when the screen is rotated.</param>
        /// <param name="dpiY">The value of the DPI along the Y axis. This value always refers to the vertical edge, even when the screen is rotated.</param>
        /// <returns>If OK, 0x00000000 | Else, 0x80070057</returns>
        [DllImport("Shcore.dll")]
        internal static extern IntPtr GetDpiForMonitor([In]IntPtr hmonitor, [In]DpiType dpiType, [Out]out uint dpiX, [Out]out uint dpiY);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        ///     Enumerates all top-level windows on the screen by passing the handle to each window, in turn, to an
        ///     application-defined callback function. <see cref="EnumWindows" /> continues until the last top-level window is
        ///     enumerated or the callback function returns FALSE.
        ///     <para>
        ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633497%28v=vs.85%29.aspx for more
        ///     information
        ///     </para>
        /// </summary>
        /// <param name="lpEnumFunc">
        ///     C++ ( lpEnumFunc [in]. Type: WNDENUMPROC )<br />A pointer to an application-defined callback
        ///     function. For more information, see
        ///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms633498%28v=vs.85%29.aspx">EnumWindowsProc</see>
        ///     .
        /// </param>
        /// <param name="lParam">
        ///     C++ ( lParam [in]. Type: LPARAM )<br />An application-defined value to be passed to the callback
        ///     function.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the return value is nonzero., <c>false</c> otherwise. If the function fails, the return value
        ///     is zero.<br />To get extended error information, call GetLastError.<br />If <see cref="EnumWindowsProc" /> returns
        ///     zero, the return value is also zero. In this case, the callback function should call SetLastError to obtain a
        ///     meaningful error code to be returned to the caller of <see cref="EnumWindows" />.
        /// </returns>
        /// <remarks>
        ///     The <see cref="EnumWindows" /> function does not enumerate child windows, with the exception of a few
        ///     top-level windows owned by the system that have the WS_CHILD style.
        ///     <para />
        ///     This function is more reliable than calling the
        ///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms633515%28v=vs.85%29.aspx">GetWindow</see>
        ///     function in a loop. An application that calls the GetWindow function to perform this task risks being caught in an
        ///     infinite loop or referencing a handle to a window that has been destroyed.<br />Note For Windows 8 and later,
        ///     EnumWindows enumerates only top-level windows of desktop apps.
        /// </remarks>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDesktopWindows(IntPtr handle, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        ///     Determines the visibility state of the specified window.
        ///     <para>
        ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633530%28v=vs.85%29.aspx for more
        ///     information. For WS_VISIBLE information go to
        ///     https://msdn.microsoft.com/en-us/library/windows/desktop/ms632600%28v=vs.85%29.aspx
        ///     </para>
        /// </summary>
        /// <param name="hWnd">C++ ( hWnd [in]. Type: HWND )<br />A handle to the window to be tested.</param>
        /// <returns>
        ///     <c>true</c> or the return value is nonzero if the specified window, its parent window, its parent's parent
        ///     window, and so forth, have the WS_VISIBLE style; otherwise, <c>false</c> or the return value is zero.
        /// </returns>
        /// <remarks>
        ///     The visibility state of a window is indicated by the WS_VISIBLE[0x10000000L] style bit. When
        ///     WS_VISIBLE[0x10000000L] is set, the window is displayed and subsequent drawing into it is displayed as long as the
        ///     window has the WS_VISIBLE[0x10000000L] style. Any drawing to a window with the WS_VISIBLE[0x10000000L] style will
        ///     not be displayed if the window is obscured by other windows or is clipped by its parent window.
        /// </remarks>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        ///     Retrieves a handle to the Shell's desktop window.
        ///     <para>
        ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633512%28v=vs.85%29.aspx for more
        ///     information
        ///     </para>
        /// </summary>
        /// <returns>
        ///     C++ ( Type: HWND )<br />The return value is the handle of the Shell's desktop window. If no Shell process is
        ///     present, the return value is NULL.
        /// </returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        internal static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        [DllImport("user32.dll")]
        internal static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        /// <summary>
        ///     Copies the text of the specified window's title bar (if it has one) into a buffer. If the specified window is a
        ///     control, the text of the control is copied. However, GetWindowText cannot retrieve the text of a control in another
        ///     application.
        ///     <para>
        ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633520%28v=vs.85%29.aspx  for more
        ///     information
        ///     </para>
        /// </summary>
        /// <param name="hWnd">
        ///     C++ ( hWnd [in]. Type: HWND )<br />A <see cref="IntPtr" /> handle to the window or control containing the text.
        /// </param>
        /// <param name="lpString">
        ///     C++ ( lpString [out]. Type: LPTSTR )<br />The <see cref="StringBuilder" /> buffer that will receive the text. If
        ///     the string is as long or longer than the buffer, the string is truncated and terminated with a null character.
        /// </param>
        /// <param name="nMaxCount">
        ///     C++ ( nMaxCount [in]. Type: int )<br /> Should be equivalent to
        ///     <see cref="StringBuilder.Length" /> after call returns. The <see cref="int" /> maximum number of characters to copy
        ///     to the buffer, including the null character. If the text exceeds this limit, it is truncated.
        /// </param>
        /// <returns>
        ///     If the function succeeds, the return value is the length, in characters, of the copied string, not including
        ///     the terminating null character. If the window has no title bar or text, if the title bar is empty, or if the window
        ///     or control handle is invalid, the return value is zero. To get extended error information, call GetLastError.<br />
        ///     This function cannot retrieve the text of an edit control in another application.
        /// </returns>
        /// <remarks>
        ///     If the target window is owned by the current process, GetWindowText causes a WM_GETTEXT message to be sent to the
        ///     specified window or control. If the target window is owned by another process and has a caption, GetWindowText
        ///     retrieves the window caption text. If the window does not have a caption, the return value is a null string. This
        ///     behavior is by design. It allows applications to call GetWindowText without becoming unresponsive if the process
        ///     that owns the target window is not responding. However, if the target window is not responding and it belongs to
        ///     the calling application, GetWindowText will cause the calling application to become unresponsive. To retrieve the
        ///     text of a control in another process, send a WM_GETTEXT message directly instead of calling GetWindowText.<br />For
        ///     an example go to
        ///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms644928%28v=vs.85%29.aspx#sending">
        ///     Sending a
        ///     Message.
        ///     </see>
        /// </remarks>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        ///     Retrieves the length, in characters, of the specified window's title bar text (if the window has a title bar). If
        ///     the specified window is a control, the function retrieves the length of the text within the control. However,
        ///     GetWindowTextLength cannot retrieve the length of the text of an edit control in another application.
        ///     <para>
        ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633521%28v=vs.85%29.aspx for more
        ///     information
        ///     </para>
        /// </summary>
        /// <param name="hWnd">C++ ( hWnd [in]. Type: HWND )<br />A <see cref="IntPtr" /> handle to the window or control.</param>
        /// <returns>
        ///     If the function succeeds, the return value is the length, in characters, of the text. Under certain
        ///     conditions, this value may actually be greater than the length of the text.<br />For more information, see the
        ///     following Remarks section. If the window has no text, the return value is zero.To get extended error information,
        ///     call GetLastError.
        /// </returns>
        /// <remarks>
        ///     If the target window is owned by the current process, <see cref="GetWindowTextLength" /> causes a
        ///     WM_GETTEXTLENGTH message to be sent to the specified window or control.<br />Under certain conditions, the
        ///     <see cref="GetWindowTextLength" /> function may return a value that is larger than the actual length of the
        ///     text.This occurs with certain mixtures of ANSI and Unicode, and is due to the system allowing for the possible
        ///     existence of double-byte character set (DBCS) characters within the text. The return value, however, will always be
        ///     at least as large as the actual length of the text; you can thus always use it to guide buffer allocation. This
        ///     behavior can occur when an application uses both ANSI functions and common dialogs, which use Unicode.It can also
        ///     occur when an application uses the ANSI version of <see cref="GetWindowTextLength" /> with a window whose window
        ///     procedure is Unicode, or the Unicode version of <see cref="GetWindowTextLength" /> with a window whose window
        ///     procedure is ANSI.<br />For more information on ANSI and ANSI functions, see Conventions for Function Prototypes.
        ///     <br />To obtain the exact length of the text, use the WM_GETTEXT, LB_GETTEXT, or CB_GETLBTEXT messages, or the
        ///     GetWindowText function.
        /// </remarks>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Retrieves the handle to the ancestor of the specified window. 
        /// </summary>
        /// <param name="hwnd">A handle to the window whose ancestor is to be retrieved. 
        /// If this parameter is the desktop window, the function returns NULL. </param>
        /// <param name="flags">The ancestor to be retrieved.</param>
        /// <returns>The return value is the handle to the ancestor window.</returns>
        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref WindowInfo pwi);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool GetTitleBarInfo(IntPtr hwnd, ref TitlebarInfo pti);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        /// <summary>
        /// Retrieves a handle to a window that has the specified relationship (Z-Order or owner) to the specified window.
        /// </summary>
        /// <remarks>The EnumChildWindows function is more reliable than calling GetWindow in a loop. An application that
        /// calls GetWindow to perform this task risks being caught in an infinite loop or referencing a handle to a window
        /// that has been destroyed.</remarks>
        /// <param name="hWnd">A handle to a window. The window handle retrieved is relative to this window, based on the
        /// value of the uCmd parameter.</param>
        /// <param name="uCmd">The relationship between the specified window and the window whose handle is to be
        /// retrieved.</param>
        /// <returns>
        /// If the function succeeds, the return value is a window handle. If no window exists with the specified relationship
        /// to the specified window, the return value is NULL. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

        //[DllImport("SHCore.dll", SetLastError = true)]
        //public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        //[DllImport("SHCore.dll", SetLastError = true)]
        //public static extern void GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS awareness);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

        #endregion

        internal delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        #region Methods

        /// <summary>
        /// Captures the screen using the SourceCopy | CaptureBlt.
        /// </summary>
        /// <param name="width">The size of the final image.</param>
        /// <param name="height">The size of the final image.</param>
        /// <param name="positionX">Source capture Left position.</param>
        /// <param name="positionY">Source capture Top position.</param>
        /// <returns>A bitmap withe the capture rectangle.</returns>
        public static BitmapSource CaptureBitmapSource(int width, int height, int positionX, int positionY)
        {
            var hDesk = GetDesktopWindow();
            var hSrce = GetWindowDC(hDesk);
            var hDest = CreateCompatibleDC(hSrce);
            var hBmp = CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = SelectObject(hDest, hBmp);

            try
            {
                var b = BitBlt(hDest, 0, 0, width, height, hSrce, positionX, positionY, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                return Imaging.CreateBitmapSourceFromHBitmap(hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                //return Image.FromHbitmap(hBmp);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        /// <summary>
        /// Captures the screen using the SourceCopy | CaptureBlt.
        /// </summary>
        /// <param name="size">The size of the final image.</param>
        /// <param name="positionX">Source capture Left position.</param>
        /// <param name="positionY">Source capture Top position.</param>
        /// <returns>A bitmap withe the capture rectangle.</returns>
        public static Image Capture(Size size, int positionX, int positionY)
        {
            var hDesk = GetDesktopWindow();
            var hSrce = GetWindowDC(hDesk);
            var hDest = CreateCompatibleDC(hSrce);
            var hBmp = CreateCompatibleBitmap(hSrce, (int)size.Width, (int)size.Height);
            var hOldBmp = SelectObject(hDest, hBmp);

            try
            {
                new System.Security.Permissions.UIPermission(System.Security.Permissions.UIPermissionWindow.AllWindows).Demand();

                var b = BitBlt(hDest, 0, 0, (int)size.Width, (int)size.Height, hSrce, positionX, positionY, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                return b ? Image.FromHbitmap(hBmp) : null;
            }
            catch (Exception ex)
            {
                //LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        public static Image CaptureWithCursor(Size size, int positionX, int positionY, out int cursorPosX, out int cursorPosY)
        {
            return CaptureWithCursor((int) size.Width, (int) size.Height, positionX, positionY, out cursorPosX, out cursorPosY);
        }

        public static Image CaptureWithCursor(int width, int height, int positionX, int positionY, out int cursorPosX, out int cursorPosY)
        {
            var hDesk = GetDesktopWindow();
            var hSrce = GetWindowDC(hDesk);
            var hDest = CreateCompatibleDC(hSrce);
            var hBmp = CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = SelectObject(hDest, hBmp);

            cursorPosX = cursorPosY = -1;

            try
            {
                new System.Security.Permissions.UIPermission(System.Security.Permissions.UIPermissionWindow.AllWindows).Demand();

                var b = BitBlt(hDest, 0, 0, width, height, hSrce, positionX, positionY, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                #region Cursor

                try
                {
                    var cursorInfo = new CursorInfo();
                    cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                    if (GetCursorInfo(out cursorInfo))
                    {
                        if (cursorInfo.flags == CursorShowing)
                        {
                            var hicon = CopyIcon(cursorInfo.hCursor);

                            if (hicon != IntPtr.Zero)
                            {
                                Iconinfo iconInfo;
                                if (GetIconInfo(hicon, out iconInfo))
                                {
                                    cursorPosX = cursorInfo.ptScreenPos.X - positionX;
                                    cursorPosY = cursorInfo.ptScreenPos.Y - positionY;

                                    if (cursorPosX > 0 && cursorPosY > 0)
                                        DrawIconEx(hDest, cursorPosX - iconInfo.xHotspot, cursorPosY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, 0, IntPtr.Zero, 0x0003);
                                }

                                DeleteObject(iconInfo.hbmColor);
                                DeleteObject(iconInfo.hbmMask);
                            }

                            DestroyIcon(hicon);
                        }

                        DeleteObject(cursorInfo.hCursor);
                    }
                }
                catch (Exception e)
                {
                    //LogWriter.Log(e, "Impossible to get screenshot of the screen");
                }

                #endregion

                return b ? Image.FromHbitmap(hBmp) : null;
            }
            catch (Exception ex)
            {
                //LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteObject(hOldBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        public static Image CaptureWindow(IntPtr handle, double scale)
        {
            var rectangle = GetWindowRect(handle);
            var posX = (int)((rectangle.X + Constants.LeftOffset) * scale);
            var posY = (int)((rectangle.Y + Constants.TopOffset) * scale);
            var width = (int)((rectangle.Width - Constants.HorizontalOffset) * scale);
            var height = (int)((rectangle.Height - Constants.VerticalOffset) * scale);

            var hDesk = GetDesktopWindow();
            var hSrce = GetWindowDC(hDesk);
            var hDest = CreateCompatibleDC(hSrce);
            var hBmp = CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = SelectObject(hDest, hBmp);

            var b = BitBlt(hDest, 0, 0, width, height, hSrce, posX, posY, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            try
            {
                return Image.FromHbitmap(hBmp);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        public static Bitmap CaptureImageCursor(ref Point point, double scale)
        {
            try
            {
                var cursorInfo = new CursorInfo();
                cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                if (!GetCursorInfo(out cursorInfo))
                    return null;

                if (cursorInfo.flags != CursorShowing)
                    return null;

                var hicon = CopyIcon(cursorInfo.hCursor);
                if (hicon == IntPtr.Zero)
                    return null;

                Iconinfo iconInfo;
                if (!GetIconInfo(hicon, out iconInfo))
                {
                    DeleteObject(hicon);
                    return null;
                }

                point.X = cursorInfo.ptScreenPos.X - iconInfo.xHotspot;
                point.Y = cursorInfo.ptScreenPos.Y - iconInfo.yHotspot;

                using (var maskBitmap = Image.FromHbitmap(iconInfo.hbmMask))
                {
                    //Is this a monochrome cursor?  
                    if (maskBitmap.Height == maskBitmap.Width * 2 && iconInfo.hbmColor == IntPtr.Zero)
                    {
                        var final = new Bitmap(maskBitmap.Width, maskBitmap.Width);
                        var hDesktop = GetDesktopWindow();
                        var dcDesktop = GetWindowDC(hDesktop);

                        using (var resultGraphics = Graphics.FromImage(final))
                        {
                            var resultHdc = resultGraphics.GetHdc();
                            var offsetX = (int)((point.X + 3) * scale);
                            var offsetY = (int)((point.Y + 3) * scale);

                            BitBlt(resultHdc, 0, 0, final.Width, final.Height, dcDesktop, offsetX, offsetY, CopyPixelOperation.SourceCopy);
                            DrawIconEx(resultHdc, 0, 0, cursorInfo.hCursor, 0, 0, 0, IntPtr.Zero, 0x0003);

                            //TODO: I have to try removing the background of this cursor capture.
                            //Native.BitBlt(resultHdc, 0, 0, final.Width, final.Height, dcDesktop, (int)point.X + 3, (int)point.Y + 3, Native.CopyPixelOperation.SourceErase);

                            //Original, ignores the screen as background.
                            //Native.BitBlt(resultHdc, 0, 0, resultBitmap.Width, resultBitmap.Height, maskHdc, 0, resultBitmap.Height, Native.CopyPixelOperation.SourceCopy); //SourceCopy
                            //Native.BitBlt(resultHdc, 0, 0, resultBitmap.Width, resultBitmap.Height, maskHdc, 0, 0, Native.CopyPixelOperation.PatInvert); //SourceInvert

                            resultGraphics.ReleaseHdc(resultHdc);
                            ReleaseDC(hDesktop, dcDesktop);
                        }

                        DeleteObject(iconInfo.hbmMask);
                        DeleteDC(dcDesktop);

                        return final;
                    }

                    DeleteObject(iconInfo.hbmColor);
                    DeleteObject(iconInfo.hbmMask);
                    DeleteObject(hicon);
                }

                var icon = Icon.FromHandle(hicon);
                return icon.ToBitmap();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get the cursor.");
            }

            return null;
        }

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

            var hdc = GetWindowDC(hWnd); //GetWindowDC((IntPtr) null); //

            GetWindowRect(hWnd, out Rect rect);

            //DwmGetWindowAttribute(hWnd, (int)DwmWindowAttribute.DwmwaExtendedFrameBounds, out rect, Marshal.SizeOf(typeof(Rect)));
            OffsetRect(ref rect, -rect.Left, -rect.Top);

            const int frameWidth = 3;

            PatBlt(hdc, rect.Left, rect.Top, rect.Right - rect.Left, frameWidth, DstInvert);

            PatBlt(hdc, rect.Left, rect.Bottom - frameWidth, frameWidth, -(rect.Bottom - rect.Top - 2 * frameWidth), DstInvert);

            PatBlt(hdc, rect.Right - frameWidth, rect.Top + frameWidth, frameWidth, rect.Bottom - rect.Top - 2 * frameWidth, DstInvert);

            PatBlt(hdc, rect.Right, rect.Bottom - frameWidth, -(rect.Right - rect.Left), frameWidth, DstInvert);
        }

        private static bool ExtendedFrameBounds(IntPtr handle, out Int32Rect rectangle)
        {
            var result = DwmGetWindowAttribute(handle, (int)DwmWindowAttribute.ExtendedFrameBounds, out Rect rect, Marshal.SizeOf(typeof(Rect)));

            rectangle = rect.ToRectangle();

            return result >= 0;
        }

        private static Int32Rect GetWindowRect(IntPtr handle)
        {
            GetWindowRect(handle, out Rect rect);
            return rect.ToRectangle();
        }

        public static Int32Rect TrueWindowRectangle(IntPtr handle)
        {
            return ExtendedFrameBounds(handle, out Int32Rect rectangle) ? rectangle : GetWindowRect(handle);
        }

        internal static Size ScreenSizeFromWindow(Window window)
        {
            return ScreenSizeFromWindow(new WindowInteropHelper(window).Handle);
        }

        internal static Size ScreenSizeFromWindow(IntPtr handle)
        {
            var pointer = MonitorFromWindow(handle, MonitorDefaultToNearest);

            var info = new MonitorInfoEx();
            GetMonitorInfo(new HandleRef(null, pointer), info);

            var rect = info.rcWork.ToRectangle();

            DeleteObject(pointer);

            return new Size(rect.Width, rect.Height);
        }

        internal static Size ScreenSizeFromPoint(int left, int top)
        {
            var pointer = MonitorFromPoint(new PointW { X = left, Y = top }, MonitorDefaultToNearest);

            var info = new MonitorInfoEx();
            GetMonitorInfo(new HandleRef(null, pointer), info);

            var rect = info.rcWork.ToRectangle();

            return new Size(rect.Width, rect.Height);
        }

        internal static string GetKnowFolderPath(Guid knownFolder, bool defaultUser = false)
        {
            string path;
            SHGetKnownFolderPath(knownFolder, (uint)KnownFolderFlags.DontVerify, new IntPtr(defaultUser ? -1 : 0), out path);
            return path;
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

        /// <summary>
        /// Returns a dictionary that contains the handle and title of all the open windows.
        /// </summary>
        /// <returns>
        /// A dictionary that contains the handle and title of all the open windows.
        /// </returns>
        internal static List<DetectedRegion> EnumerateWindows(double scale = 1)
        {
            var shellWindow = GetShellWindow();

            var windows = new List<DetectedRegion>();

            //EnumWindows(delegate (IntPtr handle, int lParam)
            EnumDesktopWindows(IntPtr.Zero, delegate (IntPtr handle, int lParam)
            {
                if (handle == shellWindow)
                    return true;

                if (!IsWindowVisible(handle))
                    return true;

                if (IsIconic(handle))
                    return true;

                var length = GetWindowTextLength(handle);

                if (length == 0)
                    return true;

                var builder = new StringBuilder(length);

                GetWindowText(handle, builder, length + 1);

                var info = new WindowInfo(false);
                GetWindowInfo(handle, ref info);

                //If disabled, ignore.
                if (((long)info.dwStyle & (uint)WindowStyles.Disabled) == (uint)WindowStyles.Disabled)
                    return true;

                var infoTile = new TitlebarInfo(false);
                GetTitleBarInfo(handle, ref infoTile);

                if ((infoTile.rgstate[0] & StateSystemInvisible) == StateSystemInvisible)
                    return true;

                if ((infoTile.rgstate[0] & StateSystemUnavailable) == StateSystemUnavailable)
                    return true;

                if ((infoTile.rgstate[0] & StateSystemOffscreen) == StateSystemOffscreen)
                    return true;

                DwmGetWindowAttribute(handle, (int)DwmWindowAttribute.Cloaked, out bool isCloacked, Marshal.SizeOf(typeof(bool)));

                if (isCloacked)
                    return true;

                DwmGetWindowAttribute(handle, (int)DwmWindowAttribute.ExtendedFrameBounds, out Rect frameBounds, Marshal.SizeOf(typeof(Rect)));

                windows.Add(new DetectedRegion(handle, frameBounds.ToRect(Util.Other.RoundUpValue(scale), scale), builder.ToString(), GetZOrder(handle)));

                return true;
            }, IntPtr.Zero);

            return windows.OrderBy(o => o.Order).ToList();
        }

        public static char? GetCharFromKey(Key key)
        {
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            var keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            var scanCode = MapVirtualKey((uint)virtualKey, MapType.MapvkVkToVsc);
            var stringBuilder = new StringBuilder(2);

            var result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);

            switch (result)
            {
                case -1:
                case 0:
                    break;
                default: //Case 1
                    return stringBuilder[0];
            }

            return null;
        }

        public static string GetSelectKeyText(Key key, ModifierKeys modifier = ModifierKeys.None, bool isUppercase = false)
        {
            var result = GetCharFromKey(key);

            if (result == null || string.IsNullOrWhiteSpace(result.ToString()) || result < 32)
            {
                switch (key)
                {
                    case Key.Oem1:
                        return GetSelectKeyText(Key.OemSemicolon);
                    case Key.Oem2:
                        return GetSelectKeyText(Key.OemQuestion);
                    case Key.Oem3:
                        return GetSelectKeyText(Key.OemTilde);
                    case Key.Oem4:
                        return GetSelectKeyText(Key.OemOpenBrackets);
                    case Key.Oem5:
                        return GetSelectKeyText(Key.OemPipe);
                    case Key.Oem6:
                        return GetSelectKeyText(Key.OemCloseBrackets);
                    case Key.Oem7:
                        return GetSelectKeyText(Key.OemComma);
                    case Key.CapsLock:
                        return "CapsLock";
                }

                if (modifier != ModifierKeys.None)
                    return modifier + " + " + key;

                return key.ToString();

                #region Try later

                /*
                    Try this later:
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

            if (modifier != ModifierKeys.None)
                return modifier + " + " + (isUppercase ? char.ToUpper(result.Value) : result);

            return (isUppercase ? char.ToUpper(result.Value) : result).ToString();
        }

        public static int GetZOrder(IntPtr hWnd)
        {
            var z = 0;
            for (var h = hWnd; h != IntPtr.Zero; h = GetWindow(h, GetWindowType.HwndPrev))
                z++;

            return z;
        }

        /// <summary>
        /// Gets the z-order for one or more windows atomically with respect to each other. 
        /// In Windows, smaller z-order is higher. If the window is not top level, the z order is returned as -1. 
        /// </summary>
        public static int[] GetZOrder(params IntPtr[] hWnds)
        {
            var z = new int[hWnds.Length];
            for (var i = 0; i < hWnds.Length; i++)
                z[i] = -1;

            var index = 0;
            var numRemaining = hWnds.Length;

            EnumWindows((wnd, param) =>
            {
                var searchIndex = Array.IndexOf(hWnds, wnd);

                if (searchIndex != -1)
                {
                    z[searchIndex] = index;
                    numRemaining--;
                    if (numRemaining == 0) return false;
                }

                index++;
                return true;
            }, IntPtr.Zero);

            return z;
        }

        public static Point GetMousePosition(double scale = 1)
        {
            var point = new PointW();
            GetCursorPos(ref point);
            return new Point(point.X / scale, point.Y / scale);
        }

        #endregion
    }

    public class Monitor
    {
        public IntPtr Handle { get; private set; }

        public Rect Bounds { get; private set; }

        public Rect WorkingArea { get; private set; }

        public string Name { get; private set; }

        public int Dpi { get; private set; }

        public bool IsPrimary { get; private set; }

        private Monitor(IntPtr monitor, IntPtr hdc)
        {
            var info = new Native.MonitorInfoEx();
            Native.GetMonitorInfo(new HandleRef(null, monitor), info);

            Handle = monitor;

            Bounds = new Rect(info.rcMonitor.Left, info.rcMonitor.Top,
                        info.rcMonitor.Right - info.rcMonitor.Left,
                        info.rcMonitor.Bottom - info.rcMonitor.Top);

            WorkingArea = new Rect(info.rcWork.Left, info.rcWork.Top,
                        info.rcWork.Right - info.rcWork.Left,
                        info.rcWork.Bottom - info.rcWork.Top);

            IsPrimary = (info.dwFlags & Native.MonitorinfofPrimary) != 0;

            Name = new string(info.szDevice).TrimEnd((char)0);

            try
            {
                uint aux = 0;
                Native.GetDpiForMonitor(monitor, Native.DpiType.Effective, out aux, out aux);
                Dpi = aux > 0 ? (int)aux : 96;
            }
            catch (Exception)
            {}

        }

        public static List<Monitor> AllMonitors
        {
            get
            {
                var closure = new MonitorEnumCallback();
                var proc = new Native.MonitorEnumProc(closure.Callback);

                Native.EnumDisplayMonitors(Native.NullHandleRef, IntPtr.Zero, proc, IntPtr.Zero);

                return closure.Monitors.Cast<Monitor>().ToList();
            }
        }

        public static List<Monitor> AllMonitorsScaled(double scale)
        {
            var monitors = AllMonitors;

            foreach (var monitor in monitors)
            {
                monitor.Bounds = new Rect(monitor.Bounds.X / scale, monitor.Bounds.Y / scale, monitor.Bounds.Width / scale, monitor.Bounds.Height / scale);
                monitor.WorkingArea = new Rect(monitor.WorkingArea.X / scale, monitor.WorkingArea.Y / scale, monitor.WorkingArea.Width / scale, monitor.WorkingArea.Height / scale);
            }

            return monitors;
        }

        private class MonitorEnumCallback
        {
            public ArrayList Monitors { get; private set; }

            public MonitorEnumCallback()
            {
                Monitors = new ArrayList();
            }

            public bool Callback(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lparam)
            {
                Monitors.Add(new Monitor(monitor, hdc));
                return true;
            }
        }
    }

    public class DetectedRegion
    {
        public IntPtr Handle { get; private set; }

        public Rect Bounds { get; set; }

        public string Name { get; private set; }

        /// <summary>
        /// The Z-Index of the window, higher means that the window will be on top.
        /// </summary>
        public int Order { get; private set; }

        public DetectedRegion(IntPtr handle, Rect bounds, string name, int order = 0)
        {
            Handle = handle;
            Bounds = bounds;
            Name = name;
            Order = order;
        }
    }
}