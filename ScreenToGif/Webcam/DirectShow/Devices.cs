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
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Text;

namespace ScreenToGif.Webcam.DirectShow;

/// <summary>
/// CLSID_SystemDeviceEnum
/// </summary>
[ComImport, Guid("62BE5D10-60EB-11d0-BD3B-00A0C911CE86")]
public class CreateDevEnum
{ }

[ComVisible(false)]
public class DsDevice : IDisposable
{
    private string _name;

    public string Name => _name ?? (_name = GetPropBagValue("FriendlyName"));

    public IMoniker Moniker { get; set; }


    public DsDevice(IMoniker mon)
    {
        Moniker = mon;
        _name = null;
    }


    /// <summary>
    /// Returns a unique identifier for a device.
    /// </summary>
    public string DevicePath
    {
        get
        {
            string s = null;

            try
            {
                Moniker.GetDisplayName(null, null, out s);
            }
            catch
            { }

            return s;
        }
    }

    /// <summary>
    /// Returns the ClassID for a device.
    /// </summary>
    public Guid ClassID
    {
        get
        {
            Moniker.GetClassID(out var g);

            return g;
        }
    }

    /// <summary>
    /// Returns an array of DsDevices of type devcat.
    /// </summary>
    /// <param name="cat">Any one of FilterCategory</param>
    public static DsDevice[] GetDevicesOfCat(Guid cat)
    {
        //Use arrayList to build the return list since it is easily resizable.
        DsDevice[] devret;
        var devs = new ArrayList();

        var enumDev = (ICreateDevEnum) new CreateDevEnum();
        var hr = enumDev.CreateClassEnumerator(cat, out var enumMon, 0);
        DsError.ThrowExceptionForHR(hr);

        //CreateClassEnumerator returns null for enumMon if there are no entries.
        if (hr != 1)
        {
            try
            {
                try
                {
                    var mon = new IMoniker[1];

                    while (enumMon.Next(1, mon, IntPtr.Zero) == 0)
                    {
                        try
                        {
                            //The devs array now owns this object. Don't release it if we are going to be successfully returning the devret array.
                            devs.Add(new DsDevice(mon[0]));
                        }
                        catch
                        {
                            Marshal.ReleaseComObject(mon[0]);
                            throw;
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(enumMon);
                }

                // Copy the ArrayList to the DsDevice[].
                devret = new DsDevice[devs.Count];
                devs.CopyTo(devret);
            }
            catch
            {
                foreach (DsDevice d in devs)
                    d.Dispose();

                throw;
            }
        }
        else
        {
            devret = new DsDevice[0];
        }

        return devret;
    }

    /// <summary>
    /// Get a specific PropertyBag value from a moniker
    /// </summary>
    /// <param name="sPropName">The name of the value to retrieve</param>
    /// <returns>String or null on error</returns>
    public string GetPropBagValue(string sPropName)
    {
        string ret;
        object bagObj = null;

        try
        {
            var bagId = typeof(IPropertyBag).GUID;
            Moniker.BindToStorage(null, null, ref bagId, out bagObj);

            var bag = (IPropertyBag)bagObj;

            var hr = bag.Read(sPropName, out object val, null);
            DsError.ThrowExceptionForHR(hr);

            ret = val as string;
        }
        catch
        {
            ret = null;
        }
        finally
        {
            if (bagObj != null)
                Marshal.ReleaseComObject(bagObj);
        }

        return ret;
    }

    public void Dispose()
    {
        if (Moniker != null)
            Marshal.ReleaseComObject(Moniker);

        Moniker = null;
    }
}

public static class DsError
{
    [DllImport("quartz.dll", CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "AMGetErrorTextW"), SuppressUnmanagedCodeSecurity]
    public static extern int AMGetErrorText(int hr, StringBuilder buf, int max);

    /// <summary>
    /// If hr has a "failed" status code (E_*), throw an exception.  Note that status
    /// messages (S_*) are not considered failure codes.  If DirectShow error text
    /// is available, it is used to build the exception, otherwise a generic com error
    /// is thrown.
    /// </summary>
    /// <param name="hr">The HRESULT to check</param>
    public static void ThrowExceptionForHR(int hr)
    {
        // If a severe error has occurred
        if (hr >= 0)
            return;

        var s = GetErrorText(hr);

        //If a string is returned, build a com error from it
        if (s != null)
            throw new COMException(s, hr);

        //No string, just use standard com error.
        Marshal.ThrowExceptionForHR(hr);
    }

    /// <summary>
    /// Returns a string describing a DS error.  Works for both error codes (values < 0) and Status codes (values >= 0)
    /// </summary>
    /// <param name="hr">HRESULT for which to get description</param>
    /// <returns>The string, or null if no error text can be found</returns>
    public static string GetErrorText(int hr)
    {
        const int maxErrorTextLen = 160;

        // Make a buffer to hold the string
        var buf = new StringBuilder(maxErrorTextLen, maxErrorTextLen);

        // If a string is returned, build a com error from it
        if (AMGetErrorText(hr, buf, maxErrorTextLen) > 0)
            return buf.ToString();

        return null;
    }
}

/// <summary>
/// From CDEF_CLASS_* defines
/// </summary>
[Flags]
public enum CDef
{
    None = 0,
    ClassDefault = 0x0001,
    BypassClassManager = 0x0002,
    ClassLegacy = 0x0004,
    MeritAboveDoNotUse = 0x0008,
    DevmonCMGRDevice = 0x0010,
    DevmonDMO = 0x0020,
    DevmonPNPDevice = 0x0040,
    DevmonFilter = 0x0080,
    DevmonSelectiveMask = 0x00f0
}

[ComVisible(true), ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ICreateDevEnum
{
    [PreserveSig]
    int CreateClassEnumerator([In, MarshalAs(UnmanagedType.LPStruct)] Guid pType, [Out] out IEnumMoniker ppEnumMoniker, [In] CDef dwFlags);
}

[ComVisible(true), ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPropertyBag
{
    [PreserveSig]
    int Read([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName, [Out, MarshalAs(UnmanagedType.Struct)] out object pVar, [In] IErrorLog pErrorLog);

    [PreserveSig]
    int Write([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName, [In, MarshalAs(UnmanagedType.Struct)] ref object pVar);
}

[ComImport, SuppressUnmanagedCodeSecurity, Guid("3127CA40-446E-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IErrorLog
{
    [PreserveSig]
    int AddError([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName, [In] System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo);
}