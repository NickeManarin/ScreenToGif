using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenToGif.Webcam.DirectShow
{
    public class CoreStreaming
    {
        [ComVisible(true), ComImport, Guid("56a86891-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPin
        {
            [PreserveSig]
            int Connect(
                [In]											IPin pReceivePin,
                [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pmt);

            [PreserveSig]
            int ReceiveConnection(
                [In]											IPin pReceivePin,
                [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pmt);

            [PreserveSig]
            int Disconnect();

            [PreserveSig]
            int ConnectedTo([Out] out IPin ppPin);

            [PreserveSig]
            int ConnectionMediaType(
                [Out, MarshalAs(UnmanagedType.LPStruct)]		AMMediaType pmt);

            [PreserveSig]
            int QueryPinInfo([Out] out PinInfo pInfo);

            [PreserveSig]
            int QueryDirection(out PinDirection pPinDir);

            [PreserveSig]
            int QueryId(
                [Out, MarshalAs(UnmanagedType.LPWStr)]		out	string Id);

            [PreserveSig]
            int QueryAccept(
                [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pmt);

            [PreserveSig]
            int EnumMediaTypes(IntPtr ppEnum);

            [PreserveSig]
            int QueryInternalConnections(IntPtr apPin, [In, Out] ref int nPin);

            [PreserveSig]
            int EndOfStream();

            [PreserveSig]
            int BeginFlush();

            [PreserveSig]
            int EndFlush();

            [PreserveSig]
            int NewSegment(long tStart, long tStop, double dRate);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode), ComVisible(false)]
        public struct PinInfo		// PIN_INFO
        {
            public IBaseFilter filter;
            public PinDirection dir;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string name;
        }

        [ComVisible(false)]
        public enum PinDirection		// PIN_DIRECTION
        {
            Input,		// PINDIR_INPUT
            Output		// PINDIR_OUTPUT
        }

        [ComVisible(true), ComImport, Guid("56a86895-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IBaseFilter
        {
            #region "IPersist Methods"
            [PreserveSig]
            int GetClassID(
                [Out]									out Guid pClassID);
            #endregion

            #region "IMediaFilter Methods"
            [PreserveSig]
            int Stop();

            [PreserveSig]
            int Pause();

            [PreserveSig]
            int Run(long tStart);

            [PreserveSig]
            int GetState(int dwMilliSecsTimeout, [Out] out int filtState);

            [PreserveSig]
            int SetSyncSource([In] IReferenceClock pClock);

            [PreserveSig]
            int GetSyncSource([Out] out IReferenceClock pClock);
            #endregion

            [PreserveSig]
            int EnumPins(
                [Out]										out IEnumPins ppEnum);

            [PreserveSig]
            int FindPin(
                [In, MarshalAs(UnmanagedType.LPWStr)]			string Id,
                [Out]										out IPin ppPin);

            [PreserveSig]
            int QueryFilterInfo(
                [Out]											FilterInfo pInfo);

            [PreserveSig]
            int JoinFilterGraph(
                [In]											IFilterGraph pGraph,
                [In, MarshalAs(UnmanagedType.LPWStr)]			string pName);

            [PreserveSig]
            int QueryVendorInfo(
                [Out, MarshalAs(UnmanagedType.LPWStr)]		out	string pVendorInfo);
        }

        [ComVisible(true), ComImport, Guid("56a86897-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IReferenceClock
        {
            [PreserveSig]
            int GetTime(out long pTime);

            [PreserveSig]
            int AdviseTime(long baseTime, long streamTime, IntPtr hEvent, out int pdwAdviseCookie);

            [PreserveSig]
            int AdvisePeriodic(long startTime, long periodTime, IntPtr hSemaphore, out int pdwAdviseCookie);

            [PreserveSig]
            int Unadvise(int dwAdviseCookie);
        }

        [ComVisible(true), ComImport, Guid("56a86892-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumPins
        {
            [PreserveSig]
            int Next(
                [In]														int cPins,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]	IPin[] ppPins,
                [Out]														out int pcFetched);

            [PreserveSig]
            int Skip([In] int cPins);
            void Reset();
            void Clone([Out] out IEnumPins ppEnum);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode), ComVisible(false)]
        public class FilterInfo		//  FILTER_INFO
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string achName;
            [MarshalAs(UnmanagedType.IUnknown)]
            public object pUnk;
        }

        [ComVisible(true), ComImport, Guid("56a8689f-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IFilterGraph
        {
            [PreserveSig]
            int AddFilter(
                [In] IBaseFilter pFilter,
                [In, MarshalAs(UnmanagedType.LPWStr)]			string pName);

            [PreserveSig]
            int RemoveFilter([In] IBaseFilter pFilter);

            [PreserveSig]
            int EnumFilters([Out] out IEnumFilters ppEnum);

            [PreserveSig]
            int FindFilterByName(
                [In, MarshalAs(UnmanagedType.LPWStr)]			string pName,
                [Out]										out IBaseFilter ppFilter);

            [PreserveSig]
            int ConnectDirect([In] IPin ppinOut, [In] IPin ppinIn,
               [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pmt);

            [PreserveSig]
            int Reconnect([In] IPin ppin);

            [PreserveSig]
            int Disconnect([In] IPin ppin);

            [PreserveSig]
            int SetDefaultSyncSource();

        }

        [ComVisible(true), ComImport, Guid("56a86893-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumFilters
        {
            [PreserveSig]
            int Next(
                [In]															uint cFilters,
                                                                            out IBaseFilter x,
                [Out]															out uint pcFetched);

            [PreserveSig]
            int Skip([In] int cFilters);
            void Reset();
            void Clone([Out] out IEnumFilters ppEnum);
        }

        [StructLayout(LayoutKind.Sequential), ComVisible(false)]
        public class AMMediaType 		//  AM_MEDIA_TYPE
        {
            public Guid majorType;
            public Guid subType;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fixedSizeSamples;
            [MarshalAs(UnmanagedType.Bool)]
            public bool temporalCompression;
            public int sampleSize;
            public Guid formatType;
            public IntPtr unkPtr;
            public int formatSize;
            public IntPtr formatPtr;
        }

        [ComVisible(true), ComImport, Guid("56a8689a-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMediaSample
        {
            [PreserveSig]
            int GetPointer(out IntPtr ppBuffer);
            [PreserveSig]
            int GetSize();

            [PreserveSig]
            int GetTime(out long pTimeStart, out long pTimeEnd);

            [PreserveSig]
            int SetTime(
                [In, MarshalAs(UnmanagedType.LPStruct)]			UtilStreaming.DsOptInt64 pTimeStart,
                [In, MarshalAs(UnmanagedType.LPStruct)]			UtilStreaming.DsOptInt64 pTimeEnd);

            [PreserveSig]
            int IsSyncPoint();
            [PreserveSig]
            int SetSyncPoint(
                [In, MarshalAs(UnmanagedType.Bool)]			bool bIsSyncPoint);

            [PreserveSig]
            int IsPreroll();
            [PreserveSig]
            int SetPreroll(
                [In, MarshalAs(UnmanagedType.Bool)]			bool bIsPreroll);

            [PreserveSig]
            int GetActualDataLength();
            [PreserveSig]
            int SetActualDataLength(int len);

            [PreserveSig]
            int GetMediaType(
                [Out, MarshalAs(UnmanagedType.LPStruct)]	out AMMediaType ppMediaType);

            [PreserveSig]
            int SetMediaType(
                [In, MarshalAs(UnmanagedType.LPStruct)]			AMMediaType pMediaType);

            [PreserveSig]
            int IsDiscontinuity();
            [PreserveSig]
            int SetDiscontinuity(
                [In, MarshalAs(UnmanagedType.Bool)]			bool bDiscontinuity);

            [PreserveSig]
            int GetMediaTime(out long pTimeStart, out long pTimeEnd);

            [PreserveSig]
            int SetMediaTime(
                [In, MarshalAs(UnmanagedType.LPStruct)]			UtilStreaming.DsOptInt64 pTimeStart,
                [In, MarshalAs(UnmanagedType.LPStruct)]			UtilStreaming.DsOptInt64 pTimeEnd);
        }

        [ComVisible(false)]
        public class DsHlp
        {
            public const int OATRUE = -1;
            public const int OAFALSE = 0;

            [DllImport("quartz.dll", CharSet = CharSet.Auto)]
            public static extern int AMGetErrorText(int hr, StringBuilder buf, int max);
        }
    }
}
