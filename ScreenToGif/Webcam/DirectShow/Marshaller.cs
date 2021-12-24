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

internal abstract class DsMarshaler : ICustomMarshaler
{
    #region Data Members

    //The cookie isn't currently being used.
    protected string ManagedCookie;

    //The managed object passed in to MarshalManagedToNative, and modified in MarshalNativeToManaged
    protected object ManagedObj;

    #endregion

    //The constructor. This is called from GetInstance (below).
    protected DsMarshaler(string cookie)
    {
        //If we get a cookie, save it.
        ManagedCookie = cookie;
    }

    //Called just before invoking the COM method.  The returned IntPtr is what goes on the stack
    //for the COM call. The input arg is the parameter that was passed to the method.
    public virtual IntPtr MarshalManagedToNative(object managedObj)
    {
        //Save off the passed-in value. Safe since we just checked the type.
        ManagedObj = managedObj;

        //Create an appropriately sized buffer, blank it, and send it to the marshaler to make the COM call with.
        var iSize = GetNativeDataSize() + 3;
        var p = Marshal.AllocCoTaskMem(iSize);

        for (var x = 0; x < iSize / 4; x++)
            Marshal.WriteInt32(p, x * 4, 0);

        return p;
    }

    // Called just after invoking the COM method.  The IntPtr is the same one that just got returned
    // from MarshalManagedToNative.  The return value is unused.
    public virtual object MarshalNativeToManaged(IntPtr pNativeData)
    {
        return ManagedObj;
    }

    // Release the (now unused) buffer
    public virtual void CleanUpNativeData(IntPtr pNativeData)
    {
        if (pNativeData != IntPtr.Zero)
            Marshal.FreeCoTaskMem(pNativeData);
    }

    // Release the (now unused) managed object
    public virtual void CleanUpManagedData(object managedObj)
    {
        ManagedObj = null;
    }

    // This routine is (apparently) never called by the marshaler.  However it can be useful.
    public abstract int GetNativeDataSize();

    // GetInstance is called by the marshaler in preparation to doing custom marshaling.  The (optional)
    // cookie is the value specified in MarshalCookie="asdf", or "" is none is specified.

    // It is commented out in this abstract class, but MUST be implemented in derived classes
    //public static ICustomMarshaler GetInstance(string cookie)
}

internal class EMTMarshaler : DsMarshaler
{
    public EMTMarshaler(string cookie) : base(cookie)
    { }

    // Called just after invoking the COM method.  The IntPtr is the same one that just got returned
    // from MarshalManagedToNative.  The return value is unused.
    public override object MarshalNativeToManaged(IntPtr pNativeData)
    {
        var emt = ManagedObj as CoreStreaming.AmMediaType[];

        for (var x = 0; x < emt.Length; x++)
        {
            //Copy in the value, and advance the pointer.
            var p = Marshal.ReadIntPtr(pNativeData, x * IntPtr.Size);

            if (p != IntPtr.Zero)
                emt[x] = (CoreStreaming.AmMediaType)Marshal.PtrToStructure(p, typeof(CoreStreaming.AmMediaType));
            else
                emt[x] = null;
        }

        return null;
    }

    //The number of bytes to marshal out.
    public override int GetNativeDataSize()
    {
        //Get the array size.
        var i = ((Array)ManagedObj).Length;

        //Multiply that times the size of a pointer.
        return i * IntPtr.Size;
    }

    //This method is called by interop to create the custom marshaler.  The (optional)
    //cookie is the value specified in MarshalCookie="asdf", or "" is none is specified.
    public static ICustomMarshaler GetInstance(string cookie)
    {
        return new EMTMarshaler(cookie);
    }
}