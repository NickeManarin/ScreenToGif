using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Webcam.DirectShow
{
    public class EditStreaming
    {
        [ComVisible(true), ComImport, Guid("6B652FFF-11FE-4fce-92AD-0266B5D7C78F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ISampleGrabber
        {
            [PreserveSig]
            int SetOneShot(
                [In, MarshalAs(UnmanagedType.Bool)]				bool OneShot);

            [PreserveSig]
            int SetMediaType(
                [In, MarshalAs(UnmanagedType.LPStruct)]			CoreStreaming.AMMediaType pmt);

            [PreserveSig]
            int GetConnectedMediaType(
                [Out, MarshalAs(UnmanagedType.LPStruct)]		CoreStreaming.AMMediaType pmt);

            [PreserveSig]
            int SetBufferSamples(
                [In, MarshalAs(UnmanagedType.Bool)]				bool BufferThem);

            [PreserveSig]
            int GetCurrentBuffer(ref int pBufferSize, IntPtr pBuffer);

            [PreserveSig]
            int GetCurrentSample(IntPtr ppSample);

            [PreserveSig]
            int SetCallback(ISampleGrabberCB pCallback, int WhichMethodToCallback);
        }

        [ComVisible(true), ComImport, Guid("0579154A-2B53-4994-B0D0-E773148EFF85"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ISampleGrabberCB
        {
            [PreserveSig]
            int SampleCB(double SampleTime, CoreStreaming.IMediaSample pSample);

            [PreserveSig]
            int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen);
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
    }
}
