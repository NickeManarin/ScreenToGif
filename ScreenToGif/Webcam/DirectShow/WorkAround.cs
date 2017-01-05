using System;
using System.Runtime.InteropServices;
namespace ScreenToGif.Webcam.DirectShow
{
    public class Workaround
    {
        /*
        works:
            CoCreateInstance( CLSID_CaptureGraphBuilder2, ..., IID_ICaptureGraphBuilder2, ...);
        doesn't (E_NOTIMPL):
            CoCreateInstance( CLSID_CaptureGraphBuilder2, ..., IID_IUnknown, ...);
        thus .NET 'Activator.CreateInstance' fails
        */

        public static object CreateDsInstance(ref Guid clsid, ref Guid riid)
        {
            IntPtr ptrIf;
            var hr = CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX.Inproc, ref riid, out ptrIf);
            if ((hr != 0) || (ptrIf == IntPtr.Zero))
                Marshal.ThrowExceptionForHR(hr);

            var iu = new Guid("00000000-0000-0000-C000-000000000046");
            IntPtr ptrXx;
            hr = Marshal.QueryInterface(ptrIf, ref iu, out ptrXx);

            var ooo = System.Runtime.Remoting.Services.EnterpriseServicesHelper.WrapIUnknownWithComObject(ptrIf);
            var ct = Marshal.Release(ptrIf);
            return ooo;
        }

        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(ref Guid clsid, IntPtr pUnkOuter, CLSCTX dwClsContext, ref Guid iid, out IntPtr ptrIf);
    }

    [Flags]
    internal enum CLSCTX
    {
        Inproc = 0x03,
        Server = 0x15,
        All = 0x17,
    }
}
