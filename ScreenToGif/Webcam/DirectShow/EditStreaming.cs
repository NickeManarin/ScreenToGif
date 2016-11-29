using System;
using System.Runtime.InteropServices;

namespace ScreenToGif.Webcam.DirectShow
{
    public class EditStreaming
    {
        [ComVisible(true), ComImport, Guid("6B652FFF-11FE-4fce-92AD-0266B5D7C78F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ISampleGrabber
        {
            [PreserveSig]
            int SetOneShot([In, MarshalAs(UnmanagedType.Bool)] bool oneShot);

            [PreserveSig]
            int SetMediaType([In, MarshalAs(UnmanagedType.LPStruct)] CoreStreaming.AMMediaType pmt);

            [PreserveSig]
            int GetConnectedMediaType([Out, MarshalAs(UnmanagedType.LPStruct)] CoreStreaming.AMMediaType pmt);

            [PreserveSig]
            int SetBufferSamples([In, MarshalAs(UnmanagedType.Bool)] bool bufferThem);

            [PreserveSig]
            int GetCurrentBuffer(ref int pBufferSize, IntPtr pBuffer);

            [PreserveSig]
            int GetCurrentSample(IntPtr ppSample);

            [PreserveSig]
            int SetCallback(ISampleGrabberCB pCallback, int whichMethodToCallback);
        }

        [ComVisible(true), ComImport, Guid("0579154A-2B53-4994-B0D0-E773148EFF85"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ISampleGrabberCB
        {
            [PreserveSig]
            int SampleCB(double sampleTime, CoreStreaming.IMediaSample pSample);

            [PreserveSig]
            int BufferCB(double sampleTime, IntPtr pBuffer, int bufferLen);
        }

        [StructLayout(LayoutKind.Sequential), ComVisible(false)]
        public class VideoInfoHeader		// VIDEOINFOHEADER
        {
            public UtilStreaming.DsRECT SrcRect;
            public UtilStreaming.DsRECT TargetRect;
            public int BitRate;
            public int BitErrorRate;
            public long AvgTimePerFrame;
            public UtilStreaming.BitmapInfoHeader BmiHeader;
        }

        [StructLayout(LayoutKind.Sequential), ComVisible(false)]
        public class VideoInfoHeader2       // VIDEOINFOHEADER2
        {
            public UtilStreaming.DsRECT SrcRect;
            public UtilStreaming.DsRECT TargetRect;
            public int BitRate;
            public int BitErrorRate;
            public long AvgTimePerFrame;
            public int InterlaceFlags;
            public int CopyProtectFlags;
            public int PictAspectRatioX;
            public int PictAspectRatioY;
            public int ControlFlags;
            public int Reserved2;
            public UtilStreaming.BitmapInfoHeader BmiHeader;
        };
    }
}
