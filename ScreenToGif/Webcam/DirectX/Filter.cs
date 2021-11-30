#region License

// ------------------------------------------------------------------
// Adapted work from DirectX.Capture
// https://www.codeproject.com/articles/3566/directx-capture-class-library
// http://creativecommons.org/licenses/publicdomain/
// -----------------------------------------------------------------

#endregion

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using ScreenToGif.Webcam.DirectShow;

namespace ScreenToGif.Webcam.DirectX;

/// <summary>
///  Represents a DirectShow filter (e.g. video capture device, compression codec).
/// </summary>
/// <remarks>
///  To save a chosen filer for later recall save the MonikerString property on the filter: 
///  <code><div style="background-color:whitesmoke;"> string savedMonikerString = myFilter.MonikerString; </div></code>
///  
///  To recall the filter create a new Filter class and pass the string to the constructor: 
///  <code><div style="background-color:whitesmoke;"> Filter mySelectedFilter = new Filter(savedMonikerString); </div></code>
/// </remarks>
public class Filter : IComparable
{
    /// <summary>
    /// Human-readable name of the filter.
    /// </summary>
    public string Name;

    /// <summary>
    /// Unique string referencing this filter. This string can be used to recreate this filter.
    /// </summary>
    public string MonikerString;

    /// <summary>
    /// Create a new filter from its moniker string.
    /// </summary>
    public Filter(string monikerString)
    {
        Name = GetName(monikerString);
        MonikerString = monikerString;
    }

    /// <summary>
    /// Create a new filter from its moniker.
    /// </summary>
    internal Filter(IMoniker moniker)
    {
        Name = GetName(moniker);
        MonikerString = GetMonikerString(moniker);
    }

    /// <summary>
    /// Retrieve the a moniker's display name (i.e. it's unique string).
    /// </summary>
    protected string GetMonikerString(IMoniker moniker)
    {
        moniker.GetDisplayName(null, null, out var s);
        return s;
    }

    /// <summary>
    /// Retrieve the human-readable name of the filter
    /// </summary>
    protected string GetName(IMoniker moniker)
    {
        object bagObj = null;

        try
        {
            var bagId = typeof(IPropertyBag).GUID;
            moniker.BindToStorage(null, null, ref bagId, out bagObj);
                
            var bag = (IPropertyBag)bagObj;
            var hr = bag.Read("FriendlyName", out var val, null);

            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);

            var ret = val as string;

            if (string.IsNullOrEmpty(ret))
                throw new NotImplementedException("Device FriendlyName");

            return ret;
        }
        catch (Exception)
        {
            return "";
        }
        finally
        {
            if (bagObj != null)
                Marshal.ReleaseComObject(bagObj); bagObj = null;
        }
    }

    /// <summary>
    /// Get a moniker's human-readable name based on a moniker string.
    /// </summary>
    protected string GetName(string monikerString)
    {
        IMoniker parser = null;
        IMoniker moniker = null;

        try
        {
            parser = GetAnyMoniker();
            parser.ParseDisplayName(null, null, monikerString, out _, out moniker);
            return GetName(parser);
        }
        finally
        {
            if (parser != null)
                Marshal.ReleaseComObject(parser);

            if (moniker != null)
                Marshal.ReleaseComObject(moniker);
        }
    }

    /// <summary>
    ///  This method gets a UCOMIMoniker object.
    /// 
    ///  HACK: The only way to create a UCOMIMoniker from a moniker 
    ///  string is to use UCOMIMoniker.ParseDisplayName(). So I 
    ///  need ANY UCOMIMoniker object so that I can call 
    ///  ParseDisplayName(). Does anyone have a better solution?
    /// 
    ///  This assumes there is at least one video compressor filter
    ///  installed on the system.
    /// </summary>
    protected IMoniker GetAnyMoniker()
    {
        var category = Uuid.FilterCategory.VideoCompressorCategory;
        object comObj = null;
        IEnumMoniker enumMon = null;
        var mon = new IMoniker[1];

        try
        {
            //Get the system device enumerator.
            var srvType = Type.GetTypeFromCLSID(Uuid.Clsid.SystemDeviceEnum);

            if (srvType == null)
                throw new NotImplementedException("System Device Enumerator");

            comObj = Activator.CreateInstance(srvType);
            var enumDev = (ICreateDevEnum)comObj;

            //Create an enumerator to find filters in category
            var hr = enumDev.CreateClassEnumerator(category, out enumMon, 0);

            if (hr != 0)
                throw new NotSupportedException("No devices of the category");

            //Get first filter.
            var f = IntPtr.Zero;
            hr = enumMon.Next(1, mon, f);

            if (hr != 0)
                mon[0] = null;

            return mon[0];
        }
        finally
        {
            if (enumMon != null)
                Marshal.ReleaseComObject(enumMon);
                
            if (comObj != null)
                Marshal.ReleaseComObject(comObj);
        }
    }

    /// <summary>
    ///  Compares the current instance with another object of the same type.
    /// </summary>
    public int CompareTo(object obj)
    {
        if (obj == null)
            return 1;
        var f = (Filter)obj;

        return string.Compare(Name, f.Name, StringComparison.Ordinal);
    }
}