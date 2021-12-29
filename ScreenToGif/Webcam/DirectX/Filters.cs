#region License

// ------------------------------------------------------------------
// Adapted work from DirectX.Capture
// https://www.codeproject.com/articles/3566/directx-capture-class-library
// http://creativecommons.org/licenses/publicdomain/
// -----------------------------------------------------------------

#endregion

using ScreenToGif.Webcam.DirectShow;

namespace ScreenToGif.Webcam.DirectX;

/// <summary>
/// Provides collections of devices and compression codecs installed on the system. 
/// </summary>
/// <example>
/// Devices and compression codecs are implemented in DirectShow as filters, see the <see cref="Filter"/> class for more information.
/// To list the available video devices:
/// <code>
/// <div style="background-color:whitesmoke;">
///   Filters filters = new Filters();
///   foreach ( Filter f in filters.VideoInputDevices )
///   {
///		 Debug.WriteLine( f.Name );
///   }
/// </div>
/// </code>
/// <seealso cref="Filter"/>
/// </example>
public class Filters
{
    /// <summary> 
    /// Collection of available video capture devices. 
    /// </summary>
    public FilterCollection VideoInputDevices = new FilterCollection(Uuid.FilterCategory.VideoInputDevice);

    ///// <summary> 
    ///// Collection of available audio capture devices. 
    ///// </summary>
    //public FilterCollection AudioInputDevices = new FilterCollection(Uuid.FilterCategory.AudioInputDevice);

    /// <summary> 
    /// Collection of available video compressors. 
    /// </summary>
    public FilterCollection VideoCompressors = new FilterCollection(Uuid.FilterCategory.VideoCompressorCategory);

    ///// <summary> 
    ///// Collection of available audio compressors. 
    ///// </summary>
    //public FilterCollection AudioCompressors = new FilterCollection(Uuid.FilterCategory.AudioCompressorCategory); 
}