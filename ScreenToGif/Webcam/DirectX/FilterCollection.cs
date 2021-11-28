#region License

// ------------------------------------------------------------------
// Adapted work from DirectX.Capture
// https://www.codeproject.com/articles/3566/directx-capture-class-library
// http://creativecommons.org/licenses/publicdomain/
// -----------------------------------------------------------------

#endregion

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using ScreenToGif.Webcam.DirectShow;

namespace ScreenToGif.Webcam.DirectX;

/// <summary>
///	 A collection of Filter objects (DirectShow filters).
///	 This is used by the <see cref="Capture"/> class to provide
///	 lists of capture devices and compression filters. This class cannot be created directly.
/// </summary>
public class FilterCollection : CollectionBase
{
    /// <summary> 
    /// Populate the collection with a list of filters from a particular category. 
    /// </summary>
    internal FilterCollection(Guid category)
    {
        GetFilters(category);
    }

    /// <summary> 
    /// Populate the InnerList with a list of filters from a particular category 
    /// </summary>
    protected void GetFilters(Guid category)
    {
        object comObj = null;
        IEnumMoniker enumMon = null;
        var mon = new IMoniker[1];

        try
        {
            //Get the system device enumerator
            var srvType = Type.GetTypeFromCLSID(Uuid.Clsid.SystemDeviceEnum);
                
            if (srvType == null)
                throw new NotImplementedException("System Device Enumerator");

            comObj = Activator.CreateInstance(srvType);
            var enumDev = (ICreateDevEnum)comObj;

            //Create an enumerator to find filters in category
            var hr = enumDev.CreateClassEnumerator(category, out enumMon, 0);

            if (hr != 0)
                return; //throw new NotSupportedException("No devices of the category");
                
            //Loop through the enumerator.
            var f = IntPtr.Zero;

            do
            {
                //Next filter.
                hr = enumMon.Next(1, mon, f);

                if (hr != 0 || mon[0] == null)
                    break;

                //Add the filter.
                var filter = new Filter(mon[0]);
                InnerList.Add(filter);

                //Release resources.
                Marshal.ReleaseComObject(mon[0]);
                mon[0] = null;
            } while (true);

            //Sort.
            InnerList.Sort();
        }
        finally
        {
            if (mon[0] != null)
                Marshal.ReleaseComObject(mon[0]);
                
            if (enumMon != null)
                Marshal.ReleaseComObject(enumMon);

            if (comObj != null)
                Marshal.ReleaseComObject(comObj);
        }
    }

    /// <summary> 
    /// Get the filter at the specified index. 
    /// </summary>
    public Filter this[int index] => (Filter) (InnerList.Count > 0 ? InnerList[index] : null);
}