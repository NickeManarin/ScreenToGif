using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ScreenToGif.Webcam.DirectShow;

namespace ScreenToGif.Webcam.DirectX
{
    /// <summary>
    ///	 A collection of Filter objects (DirectShow filters).
    ///	 This is used by the <see cref="Capture"/> class to provide
    ///	 lists of capture devices and compression filters. This class
    ///	 cannot be created directly.
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
            int hr;
            object comObj = null;
            ICreateDevEnum enumDev = null;
            UCOMIEnumMoniker enumMon = null;
            var mon = new UCOMIMoniker[1];

            try
            {
                // Get the system device enumerator
                Type srvType = Type.GetTypeFromCLSID(Uuid.Clsid.SystemDeviceEnum);
                if (srvType == null)
                    throw new NotImplementedException("System Device Enumerator");
                comObj = Activator.CreateInstance(srvType);
                enumDev = (ICreateDevEnum)comObj;

                // Create an enumerator to find filters in category
                hr = enumDev.CreateClassEnumerator(ref category, out enumMon, 0);
                if (hr != 0)
                    return;
                    //throw new NotSupportedException("No devices of the category");


                // Loop through the enumerator
                int f;

                do
                {
                    // Next filter
                    hr = enumMon.Next(1, mon, out f);

                    if ((hr != 0) || (mon[0] == null))
                        break;

                    // Add the filter
                    var filter = new Filter(mon[0]);
                    InnerList.Add(filter);

                    // Release resources
                    Marshal.ReleaseComObject(mon[0]);
                    mon[0] = null;
                } while (true);

                // Sort
                InnerList.Sort();
            }
            finally
            {
                enumDev = null;
                if (mon[0] != null)
                    Marshal.ReleaseComObject(mon[0]); mon[0] = null;
                if (enumMon != null)
                    Marshal.ReleaseComObject(enumMon); enumMon = null;
                if (comObj != null)
                    Marshal.ReleaseComObject(comObj); comObj = null;
            }
        }

        /// <summary> 
        /// Get the filter at the specified index. 
        /// </summary>
        public Filter this[int index]
        {
            get { return ((Filter)InnerList[index]); }
        }
    }
}
