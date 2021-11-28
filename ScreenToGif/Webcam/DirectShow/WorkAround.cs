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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ScreenToGif.Webcam.DirectShow;

public class Workaround
{
    /*
    works:
        CoCreateInstance( CLSID_CaptureGraphBuilder2, ..., IID_ICaptureGraphBuilder2, ...);
    doesn't (E_NOTIMPL):
        CoCreateInstance( CLSID_CaptureGraphBuilder2, ..., IID_IUnknown, ...);
    thus .NET 'Activator.CreateInstance' fails
    */

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern object InternalWrapIUnknownWithComObject(IntPtr i);

    public static object CreateDsInstance(ref Guid clsid, ref Guid riid)
    {
        var hr = CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX.Inproc, ref riid, out var ptrIf);

        if (hr != 0 || ptrIf == IntPtr.Zero)
            Marshal.ThrowExceptionForHR(hr);

        var iu = new Guid("00000000-0000-0000-C000-000000000046");
        hr = Marshal.QueryInterface(ptrIf, ref iu, out _);

        var ooo = InternalWrapIUnknownWithComObject(ptrIf); //System.Runtime.Remoting.Services.EnterpriseServicesHelper.WrapIUnknownWithComObject(ptrIf);
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