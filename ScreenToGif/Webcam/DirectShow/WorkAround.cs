using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
            int hr = CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX.Inproc, ref riid, out ptrIf);
            if ((hr != 0) || (ptrIf == IntPtr.Zero))
                Marshal.ThrowExceptionForHR(hr);

            Guid iu = new Guid("00000000-0000-0000-C000-000000000046");
            IntPtr ptrXX;
            hr = Marshal.QueryInterface(ptrIf, ref iu, out ptrXX);

            object ooo = System.Runtime.Remoting.Services.EnterpriseServicesHelper.WrapIUnknownWithComObject(ptrIf);
            int ct = Marshal.Release(ptrIf);
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
