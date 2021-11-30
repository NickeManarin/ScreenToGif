using System;
using System.Runtime.InteropServices;

namespace ScreenToGif.Webcam.DirectShow;

public class ExtendStreaming
{
    [ComVisible(true), ComImport, Guid("56a868a9-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IGraphBuilder : CoreStreaming.IFilterGraph
    {
        #region "IFilterGraph Methods"

        [PreserveSig]
        new int AddFilter([In] CoreStreaming.IBaseFilter pFilter, [In, MarshalAs(UnmanagedType.LPWStr)] string pName);

        [PreserveSig]
        new int RemoveFilter([In] CoreStreaming.IBaseFilter pFilter);

        [PreserveSig]
        new int EnumFilters([Out] out CoreStreaming.IEnumFilters ppEnum);

        [PreserveSig]
        new int FindFilterByName([In, MarshalAs(UnmanagedType.LPWStr)] string pName, [Out] out CoreStreaming.IBaseFilter ppFilter);

        [PreserveSig]
        new int ConnectDirect([In] CoreStreaming.IPin ppinOut, [In] CoreStreaming.IPin ppinIn, [In, MarshalAs(UnmanagedType.LPStruct)] CoreStreaming.AmMediaType pmt);

        [PreserveSig]
        new int Reconnect([In] CoreStreaming.IPin ppin);

        [PreserveSig]
        new int Disconnect([In] CoreStreaming.IPin ppin);

        [PreserveSig]
        new int SetDefaultSyncSource();

        #endregion

        [PreserveSig]
        int Connect([In] CoreStreaming.IPin ppinOut, [In] CoreStreaming.IPin ppinIn);

        [PreserveSig]
        int Render([In] CoreStreaming.IPin ppinOut);

        [PreserveSig]
        int RenderFile([In, MarshalAs(UnmanagedType.LPWStr)] string lpcwstrFile, [In, MarshalAs(UnmanagedType.LPWStr)] string lpcwstrPlayList);

        [PreserveSig]
        int AddSourceFilter([In, MarshalAs(UnmanagedType.LPWStr)] string lpcwstrFileName, [In, MarshalAs(UnmanagedType.LPWStr)] string lpcwstrFilterName, [Out] out CoreStreaming.IBaseFilter ppFilter);

        [PreserveSig]
        int SetLogFile(IntPtr hFile);

        [PreserveSig]
        int Abort();

        [PreserveSig]
        int ShouldOperationContinue();
    }

    [ComVisible(true), ComImport, Guid("93E5A4E0-2D50-11d2-ABFA-00A0C9C6E38D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICaptureGraphBuilder2
    {
        [PreserveSig]
        int SetFiltergraph([In] IGraphBuilder pfg);

        [PreserveSig]
        int GetFiltergraph([Out] out IGraphBuilder ppfg);

        [PreserveSig]
        int SetOutputFileName([In, MarshalAs(UnmanagedType.LPStruct)] Guid pType, [In, MarshalAs(UnmanagedType.LPWStr)] string lpstrFile, [Out] out CoreStreaming.IBaseFilter ppbf, [Out] out IFileSinkFilter ppSink);

        [PreserveSig]
        int FindInterface([In, MarshalAs(UnmanagedType.LPStruct)] Util.DsGuid pCategory, [In, MarshalAs(UnmanagedType.LPStruct)] Util.DsGuid pType, [In] CoreStreaming.IBaseFilter pbf, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppint);

        [PreserveSig]
        int RenderStream([In, MarshalAs(UnmanagedType.LPStruct)] Util.DsGuid PinCategory, [In, MarshalAs(UnmanagedType.LPStruct)] Util.DsGuid MediaType, [In, MarshalAs(UnmanagedType.IUnknown)] object pSource, [In] CoreStreaming.IBaseFilter pfCompressor, [In] CoreStreaming.IBaseFilter pfRenderer);

        [PreserveSig]
        int ControlStream([In, MarshalAs(UnmanagedType.LPStruct)] Guid pCategory, [In, MarshalAs(UnmanagedType.LPStruct)] Util.DsGuid pType, [In, MarshalAs(UnmanagedType.Interface)] CoreStreaming.IBaseFilter pFilter, [In] Util.DsLong pstart, [In] Util.DsLong pstop, [In] short wStartCookie, [In] short wStopCookie);

        [PreserveSig]
        int AllocCapFile([In, MarshalAs(UnmanagedType.LPWStr)] string lpstrFile, [In] long dwlSize);

        [PreserveSig]
        int CopyCaptureFile([In, MarshalAs(UnmanagedType.LPWStr)] string lpwstrOld, [In, MarshalAs(UnmanagedType.LPWStr)] string lpwstrNew, [In, MarshalAs(UnmanagedType.Bool)] int fAllowEscAbort, [In] IAMCopyCaptureFileProgress pFilter);

        [PreserveSig]
        int FindPin([In, MarshalAs(UnmanagedType.IUnknown)] object pSource, [In] CoreStreaming.PinDirection pindir, [In, MarshalAs(UnmanagedType.LPStruct)] Util.DsGuid pCategory, [In, MarshalAs(UnmanagedType.LPStruct)] Util.DsGuid mediaType, [In, MarshalAs(UnmanagedType.Bool)] bool fUnconnected, [In] int num, [Out, MarshalAs(UnmanagedType.Interface)] out CoreStreaming.IPin ppPin);
    }

    [ComVisible(true), ComImport, Guid("a2104830-7c70-11cf-8bce-00aa00a3f1a6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFileSinkFilter
    {
        [PreserveSig]
        int SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [In, MarshalAs(UnmanagedType.LPStruct)] CoreStreaming.AmMediaType pmt);

        [PreserveSig]
        int GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] out string pszFileName, [Out, MarshalAs(UnmanagedType.LPStruct)] CoreStreaming.AmMediaType pmt);
    }

    [ComVisible(true), ComImport, Guid("670d1d20-a068-11d0-b3f0-00aa003761c5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAMCopyCaptureFileProgress
    {
        [PreserveSig]
        int Progress(int iProgress);
    }

    [ComVisible(true), ComImport, Guid("C6E13340-30AC-11d0-A18C-00A0C9118956"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAMStreamConfig
    {
        [PreserveSig]
        int SetFormat([In, MarshalAs(UnmanagedType.LPStruct)] CoreStreaming.AmMediaType pmt);

        [PreserveSig]
        int GetFormat([Out] out IntPtr pmt);

        [PreserveSig]
        int GetNumberOfCapabilities(out int piCount, out int piSize);

        [PreserveSig]
        int GetStreamCaps([In] int iIndex, [Out] out CoreStreaming.AmMediaType pmt, [In] IntPtr pSCC);
    }
}