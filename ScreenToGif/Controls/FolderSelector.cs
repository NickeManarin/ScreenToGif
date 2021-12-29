using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenToGif.Controls;

/// <summary>
/// Folder selector, vista-style.
/// </summary>
/// <remarks>
/// Source:
/// https://www.magnumdb.com/search?q=IShellItem
/// https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ifiledialog
/// https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/ne-shobjidl_core-_fileopendialogoptions
/// https://docs.microsoft.com/en-us/windows/win32/api/shobjidl_core/ne-shobjidl_core-sigdn
/// </remarks>
internal class FolderSelector
{
    #region Native

    #region Constants

    /// <summary>
    /// Present an Open dialog that offers a choice of folders rather than files.
    /// </summary>
    public const uint DialogPickFolders = 0x00000020;

    /// <summary>
    /// Ensures that returned items are file system items (SFGAO_FILESYSTEM).
    /// Note that this does not apply to items returned by IFileDialog::GetCurrentSelection.
    /// </summary>
    public const uint DialogForceFileSystem = 0x00000040;

    /// <summary>
    /// Do not check for situations that would prevent an application from opening the selected file, such as sharing violations or access denied errors.
    /// </summary>
    public const uint DialogNoValidade = 0x00000100;

    /// <summary>
    /// Do not test whether creation of the item as specified in the Save dialog will be successful.
    /// If this flag is not set, the calling application must handle errors, such as denial of access, discovered when the item is created.
    /// </summary>
    public const uint DialogNoTestFileCreate = 0x00010000;

    /// <summary>
    /// Do not add the item being opened or saved to the recent documents list (SHAddToRecentDocs).
    /// </summary>
    public const uint DialogDontAddToRecent = 0x02000000;

    /// <summary>
    /// Ok return status.
    /// </summary>
    public const uint StatusOk = 0x0000;

    /// <summary>
    /// Returns the item's file system path, if it has one. Only items that report SFGAO_FILESYSTEM have a file system path.
    /// When an item does not have a file system path, a call to IShellItem::GetDisplayName on that item will fail.
    /// In UI this name is suitable for display to the user in some cases, but note that it might not be specified for all items.
    /// </summary>
    public const uint DisplayFileSysPath = 0x80058000;

    #endregion

    #region COM Imports

    [ComImport, ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate), Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    internal class FileOpenDialogRCW { }

    [ComImport, Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialog
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), PreserveSig]
        uint Show([In, Optional] IntPtr hwndOwner); //IModalWindow 

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr rgFilterSpec);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetFileTypeIndex([In] uint iFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetFileTypeIndex(out uint piFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint Unadvise([In] uint dwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetOptions([In] uint fos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetOptions(out uint fos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint Close([MarshalAs(UnmanagedType.Error)] uint hr);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetClientGuid([In] ref Guid guid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint ClearClientData();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
    }

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint BindToHandler([In] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IntPtr ppvOut);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetDisplayName([In] uint sigdnName, out IntPtr ppszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
    }

    #endregion

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the descriptive text displayed above the tree view control in the dialog box.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the selected folder path.
    /// If some path is set before opening the dialog, that path is used as a folder that is always
    /// selected when the dialog is opened, regardless of previous user action.
    /// </summary>
    public string SelectedPath { get; set; }

    /// <summary>
    /// Default folder to be used if no recent folder available.
    /// </summary>
    public string DefaultFolder { get; set; }

    #endregion
        
    public bool ShowDialog(IWin32Window owner = null)
    {
        //ReSharper disable once SuspiciousTypeConversion.Global
        var frm = (IFileDialog) new FileOpenDialogRCW();
            
        //Set folder picker options.
        frm.GetOptions(out var options);
        options |= DialogPickFolders | DialogForceFileSystem | DialogNoValidade | DialogNoTestFileCreate | DialogDontAddToRecent;
        frm.SetOptions(options);
            
        if (!string.IsNullOrWhiteSpace(Description))
            frm.SetTitle(Description);

        if (SelectedPath != null)
        {
            //IShellItem
            var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");

            if (SHCreateItemFromParsingName(SelectedPath, IntPtr.Zero, ref riid, out var directoryShellItem) == StatusOk)
                frm.SetFolder(directoryShellItem);
        }

        if (DefaultFolder != null)
        {
            //IShellItem
            var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");

            if (SHCreateItemFromParsingName(DefaultFolder, IntPtr.Zero, ref riid, out var directoryShellItem) == StatusOk)
                frm.SetDefaultFolder(directoryShellItem);
        }

        if (frm.Show(owner?.Handle ?? IntPtr.Zero) != StatusOk || frm.GetResult(out var shellItem) != StatusOk || shellItem.GetDisplayName(DisplayFileSysPath, out var pszString) != StatusOk || pszString == IntPtr.Zero)
            return false;
            
        try
        {
            SelectedPath = Marshal.PtrToStringAuto(pszString);
            return true;
        }
        finally
        {
            Marshal.FreeCoTaskMem(pszString);
        }
    }
}