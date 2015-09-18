using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ScreenToGif.Webcam.DirectShow;

namespace ScreenToGif.Webcam.DirectX
{
    /// <summary>
    ///  Represents a DirectShow filter (e.g. video capture device, 
    ///  compression codec).
    /// </summary>
    /// <remarks>
    ///  To save a chosen filer for later recall
    ///  save the MonikerString property on the filter: 
    ///  <code><div style="background-color:whitesmoke;">
    ///   string savedMonikerString = myFilter.MonikerString;
    ///  </div></code>
    ///  
    ///  To recall the filter create a new Filter class and pass the 
    ///  string to the constructor: 
    ///  <code><div style="background-color:whitesmoke;">
    ///   Filter mySelectedFilter = new Filter(savedMonikerString);
    ///  </div></code>
    /// </remarks>
    public class Filter : IComparable
    {
        /// <summary> Human-readable name of the filter </summary>
        public string Name;

        /// <summary> Unique string referencing this filter. This string can be used to recreate this filter. </summary>
        public string MonikerString;

        /// <summary> Create a new filter from its moniker string. </summary>
        public Filter(string monikerString)
        {
            Name = GetName(monikerString);
            MonikerString = monikerString;
        }

        /// <summary> Create a new filter from its moniker </summary>
        internal Filter(UCOMIMoniker moniker)
        {
            Name = GetName(moniker);
            MonikerString = GetMonikerString(moniker);
        }

        /// <summary> Retrieve the a moniker's display name (i.e. it's unique string) </summary>
        protected string GetMonikerString(UCOMIMoniker moniker)
        {
            string s;
            moniker.GetDisplayName(null, null, out s);
            return (s);
        }

        /// <summary> Retrieve the human-readable name of the filter </summary>
        protected string GetName(UCOMIMoniker moniker)
        {
            object bagObj = null;
            IPropertyBag bag = null;

            try
            {
                Guid bagId = typeof(IPropertyBag).GUID;
                moniker.BindToStorage(null, null, ref bagId, out bagObj);
                bag = (IPropertyBag)bagObj;
                object val = "";
                int hr = bag.Read("FriendlyName", ref val, IntPtr.Zero);

                if (hr != 0)
                    Marshal.ThrowExceptionForHR(hr);

                var ret = val as string;

                if (string.IsNullOrEmpty(ret))
                    throw new NotImplementedException("Device FriendlyName");
                return (ret);
            }
            catch (Exception)
            {
                return ("");
            }
            finally
            {
                bag = null;
                if (bagObj != null)
                    Marshal.ReleaseComObject(bagObj); bagObj = null;
            }
        }

        /// <summary> Get a moniker's human-readable name based on a moniker string. </summary>
        protected string GetName(string monikerString)
        {
            UCOMIMoniker parser = null;
            UCOMIMoniker moniker = null;

            try
            {
                parser = GetAnyMoniker();
                int eaten;
                parser.ParseDisplayName(null, null, monikerString, out eaten, out moniker);
                return (GetName(parser));
            }
            finally
            {
                if (parser != null)
                    Marshal.ReleaseComObject(parser); parser = null;
                if (moniker != null)
                    Marshal.ReleaseComObject(moniker); moniker = null;
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
        protected UCOMIMoniker GetAnyMoniker()
        {
            Guid category = Uuid.FilterCategory.VideoCompressorCategory;
            int hr;
            object comObj = null;
            ICreateDevEnum enumDev = null;
            UCOMIEnumMoniker enumMon = null;
            UCOMIMoniker[] mon = new UCOMIMoniker[1];

            try
            {
                // Get the system device enumerator
                Type srvType = Type.GetTypeFromCLSID(Uuid.Clsid.SystemDeviceEnum);
                if (srvType == null)
                    throw new NotImplementedException("System Device Enumerator");
                comObj = Activator.CreateInstance(srvType);
                enumDev = (ICreateDevEnum)comObj;

                //Create an enumerator to find filters in category
                hr = enumDev.CreateClassEnumerator(ref category, out enumMon, 0);
                if (hr != 0)
                    throw new NotSupportedException("No devices of the category");

                // Get first filter
                int f;
                hr = enumMon.Next(1, mon, out f);
                if ((hr != 0))
                    mon[0] = null;

                return (mon[0]);
            }
            finally
            {
                enumDev = null;
                if (enumMon != null)
                    Marshal.ReleaseComObject(enumMon); enumMon = null;
                if (comObj != null)
                    Marshal.ReleaseComObject(comObj); comObj = null;
            }
        }

        /// <summary>
        ///  Compares the current instance with another object of 
        ///  the same type.
        /// </summary>
        public int CompareTo(object obj)
        {
            if (obj == null)
                return (1);
            var f = (Filter)obj;

            return (String.Compare(this.Name, f.Name, StringComparison.Ordinal));
        }
    }
}
