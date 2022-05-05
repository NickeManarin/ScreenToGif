using ScreenToGif.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using ScreenToGif.Domain.Models;
using ScreenToGif.Model;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Util;

/// <summary>
/// Other helper methods.
/// </summary>
public static class Other
{
    //private static string _assemblyShortName;

    ///// <summary>
    ///// Helper method for generating a "pack://" URI for a given relative file based on the
    ///// assembly that this class is in.
    ///// </summary>
    //public static Uri MakePackUri(string relativeFile)
    //{
    //    var uriString = "pack://application:,,,/" + AssemblyShortName + ";component/" + relativeFile;
    //    return new Uri(uriString);
    //}

    //private static string AssemblyShortName
    //{
    //    get
    //    {
    //        if (_assemblyShortName != null)
    //            return _assemblyShortName;

    //        var a = typeof(Global).Assembly;

    //        //Pull out the short name.
    //        _assemblyShortName = a.ToString().Split(',')[0];

    //        return _assemblyShortName;
    //    }
    //}

    public static Point TransformToScreen(Point point, Visual relativeTo)
    {
        var hwndSource = PresentationSource.FromVisual(relativeTo) as HwndSource;
        var root = hwndSource.RootVisual;

        // Translate the point from the visual to the root.
        var transformToRoot = relativeTo.TransformToAncestor(root);

        var pointRoot = transformToRoot.Transform(point);

        // Transform the point from the root to client coordinates.
        var m = Matrix.Identity;

        var transform = VisualTreeHelper.GetTransform(root);

        if (transform != null)
        {
            m = Matrix.Multiply(m, transform.Value);
        }

        var offset = VisualTreeHelper.GetOffset(root);
        m.Translate(offset.X, offset.Y);

        var pointClient = m.Transform(pointRoot);

        // Convert from “device-independent pixels” into pixels.
        pointClient = hwndSource.CompositionTarget.TransformToDevice.Transform(pointClient);

        var pointClientPixels = new PointW();
        pointClientPixels.X = (0 < pointClient.X) ? (int)(pointClient.X + 0.5) : (int)(pointClient.X - 0.5);
        pointClientPixels.Y = (0 < pointClient.Y) ? (int)(pointClient.Y + 0.5) : (int)(pointClient.Y - 0.5);

        // Transform the point into screen coordinates.
        var pointScreenPixels = pointClientPixels;
        User32.ClientToScreen(hwndSource.Handle, ref pointScreenPixels);

        //Native.GetCurrentPositionEx(hwndSource.Handle, out pointScreenPixels);
        //Native.GetWindowOrgEx(hwndSource.Handle, out pointScreenPixels);

        return new Point(pointScreenPixels.X, pointScreenPixels.Y);
    }

    public static string GetTextResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var result = "";

        try
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();

                    reader.Close();
                }

                stream.Close();
            }
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Resource Loading", resourceName);
        }

        return result;
    }

    private static Size MeasureString(this TextBlock textBlock)
    {
        var formattedText = new FormattedText(textBlock.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch), textBlock.FontSize, Brushes.Black, 96d);

        return new Size(formattedText.Width, formattedText.Height);
    }

    internal static Size Scale(this Size size, double scale)
    {
        return new Size(Math.Round(size.Width * scale, MidpointRounding.AwayFromZero), Math.Round(size.Height * scale, MidpointRounding.AwayFromZero));
    }

    internal static Point Scale(this Point point, double scale)
    {
        return new Point(Math.Round(point.X * scale, MidpointRounding.AwayFromZero), Math.Round(point.Y * scale, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Gets the DPI of the current window.
    /// </summary>
    /// <param name="window">The Window.</param>
    /// <returns>The DPI of the given Window.</returns>
    public static double Dpi(this Window window)
    {
        var source = PresentationSource.FromVisual(window);

        if (source?.CompositionTarget != null)
            return 96d * source.CompositionTarget.TransformToDevice.M11;

        return 96d;
    }

    /// <summary>
    /// Gets the DPI of the system.
    /// </summary>
    /// <returns>The DPI of the system.</returns>
    public static double DpiOfSystem()
    {
        using (var source = new HwndSource(new HwndSourceParameters()))
            return 96d * (source.CompositionTarget?.TransformToDevice.M11 ?? 1D);
    }

    /// <summary>
    /// Gets the scale of the current window.
    /// </summary>
    /// <param name="window">The Window.</param>
    /// <returns>The scale of the given Window.</returns>
    public static double Scale(this Visual window)
    {
        var source = PresentationSource.FromVisual(window);

        if (source?.CompositionTarget != null)
            return source.CompositionTarget.TransformToDevice.M11;

        return 1d;
    }

    /// <summary>
    /// Gets the scale of the system.
    /// </summary>
    /// <returns>The scale of the system.</returns>
    public static double ScaleOfSystem()
    {
        using (var source = new HwndSource(new HwndSourceParameters()))
            return source.CompositionTarget?.TransformToDevice.M11 ?? 1D;
    }

    public static List<DetectedRegion> AdjustPosition(this List<DetectedRegion> list, double x, double y)
    {
        foreach (var region in list)
            region.Bounds = new Rect(new Point(region.Bounds.X - x, region.Bounds.Y - y), region.Bounds.Size);

        return list;
    }

    [Obsolete("Use LocalizationHelper.Get() instead")]
    public static string TextResource(this FrameworkElement visual, string key, string defaultValue = "")
    {
        return visual.TryFindResource(key) as string ?? defaultValue;
    }

    public static Brush RandomBrush()
    {
        var rnd = new Random();

        var brushesType = typeof(Brushes);

        var properties = brushesType.GetProperties();

        var random = rnd.Next(properties.Length);

        return (Brush)properties[random].GetValue(null, null);
    }

    public static void CopyPropertiesTo<T, TU>(this T source, TU dest)
    {
        var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
        var destProps = typeof(TU).GetProperties().Where(x => x.CanWrite).ToList();

        foreach (var sourceProp in sourceProps)
        {
            if (destProps.All(x => x.Name != sourceProp.Name))
                continue;

            var p = destProps.First(x => x.Name == sourceProp.Name);

            if (p.CanWrite)
                p.SetValue(dest, sourceProp.GetValue(source, null), null);
        }
    }

    #region List

    public static List<FrameInfo> CopyList(this List<FrameInfo> target)
    {
        return new List<FrameInfo>(target.Select(s => new FrameInfo(s.Path, s.Delay, s.CursorX, s.CursorY, s.ButtonClicked,
            s.KeyList != null ? new List<IKeyGesture>(s.KeyList.Select(y => new SimpleKeyGesture(y.Key, y.Modifiers, y.IsUppercase, y.IsInjected))) : null, s.Index)));
    }

    /// <summary>
    /// Creates an index list based on the start and end indexes (positions).
    /// </summary>
    /// <param name="start">The start index.</param>
    /// <param name="end">The end index. If it's a lower value than the start index, the start becomes the end and vice-versa.</param>
    /// <returns>A list of ordered integers.</returns>
    public static List<int> ListOfIndexesOld(int start, int end)
    {
        if (start > end)
            return Enumerable.Range(end, start - end + 1).ToList();

        return Enumerable.Range(start, end - start + 1).ToList();
    }

    /// <summary>
    /// Creates an index list based on the start and end indexes (positions).
    /// </summary>
    /// <param name="start">The start index.</param>
    /// <param name="quantity">The quantity indexes to create.</param>
    /// <returns>A list of ordered integers.</returns>
    public static List<int> ListOfIndexes(int start, int quantity)
    {
        //if (start > end)
        //    return Enumerable.Range(end, start - end + 1).ToList();

        return Enumerable.Range(start, quantity).ToList();
    }

    /// <summary>
    /// Makes a Yo-yo effect with the given List (List + Reverted List)
    /// </summary>
    /// <param name="list">The list to apply the effect</param>
    /// <returns>A List with the Yo-yo effect</returns>
    public static List<FrameInfo> Yoyo(List<FrameInfo> list)
    {
        var listReverted = new List<FrameInfo>(list);
        listReverted.Reverse();

        var currentFolder = Path.GetDirectoryName(list[0].Path);

        foreach (var frame in listReverted)
        {
            var newPath = Path.Combine(currentFolder, list.Count + " Y " + DateTime.Now.ToString("yy MM dd hh mm ss fff") + ".png");

            File.Copy(frame.Path, newPath);

            list.Add(new FrameInfo(newPath, frame.Delay, frame.CursorX, frame.CursorY, frame.ButtonClicked, frame.KeyList, frame.Index));
        }

        return list;
    }

    public static List<FrameInfo> Move(this List<FrameInfo> list, int oldIndex, int newIndex)
    {
        //Saves the current item on a temp variable.
        var item = list[oldIndex];

        list.RemoveAt(oldIndex);
        list.Insert(newIndex, item);

        return list;
    }

    public static List<int> Move(this List<int> list, int oldIndex, int newIndex)
    {
        //Saves the current item on a temp variable.
        var item = list[oldIndex];

        list.RemoveAt(oldIndex);

        //The actual index could have shifted due to the removal.
        if (newIndex > oldIndex)
            newIndex--;

        list.Insert(newIndex, item);

        return list;
    }

    #endregion

    #region Event Helper

    /// <summary>
    /// Removes all event handlers subscribed to the specified routed event from the specified element.
    /// http://stackoverflow.com/a/12618521/1735672
    /// </summary>
    /// <param name="element">The UI element on which the routed event is defined.</param>
    /// <param name="routedEvent">The routed event for which to remove the event handlers.</param>
    public static void RemoveRoutedEventHandlers(UIElement element, RoutedEvent routedEvent)
    {
        try
        {
            //Get the EventHandlersStore instance which holds event handlers for the specified element.
            //The EventHandlersStore class is declared as internal.
            var eventHandlersStoreProperty = typeof(UIElement).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);

            var eventHandlersStore = eventHandlersStoreProperty?.GetValue(element, null);

            //If no event handlers are subscribed, eventHandlersStore will be null.
            if (eventHandlersStore == null)
                return;

            //Invoke the GetRoutedEventHandlers method on the EventHandlersStore instance for getting an array of the subscribed event handlers.
            var getRoutedEventHandlers = eventHandlersStore.GetType().GetMethod("GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var routedEventHandlers = (RoutedEventHandlerInfo[])getRoutedEventHandlers.Invoke(eventHandlersStore, new object[] { routedEvent });

            //Iteratively remove all routed event handlers from the element.
            foreach (var routedEventHandler in routedEventHandlers)
                element.RemoveHandler(routedEvent, routedEventHandler.Handler);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Removing event handlers");
        }
    }

    #endregion

    #region Dependencies

    /// <summary>
    /// When dealing with relative paths, the app will fails to point to the right folder when starting it via the "Open with..." or automatic startup methods.
    /// </summary>
    public static string AdjustPath(string path)
    {
        //If the path is relative, File.Exists() was returning C:\\Windows\\System32\ffmpeg.exe when the app was launched from the "Open with" context menu.
        //So, in order to get the correct location, I need to combine the current base directory with the relative path.
        if (!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path))
        {
            var adjusted = path.StartsWith("." + Path.AltDirectorySeparatorChar) ? path.TrimStart('.', Path.AltDirectorySeparatorChar) :
                path.StartsWith("." + Path.DirectorySeparatorChar) ? path.TrimStart('.', Path.DirectorySeparatorChar) : path;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, adjusted);
        }

        return path;
    }

    public static bool IsFfmpegPresent(bool ignoreEnvironment = false, bool ignoreEmpty = false)
    {
        //If the path is relative, File.Exists() was returning C:\\Windows\\System32\ffmpeg.exe when the app was launched from the "Open with" context menu.
        //So, in order to get the correct location, I need to combine the current base directory with the relative path.
        var realPath = AdjustPath(UserSettings.All.FfmpegLocation);

        //File location already chosen or detected.
        if (!string.IsNullOrWhiteSpace(realPath) && File.Exists(realPath))
            return true;

        //The path was not selected, it may be located inside a common folder.
        if (!ignoreEmpty && string.IsNullOrWhiteSpace(UserSettings.All.FfmpegLocation))
        {
            //Same path as application.
            if (File.Exists(AdjustPath("ffmpeg.exe")))
            {
                UserSettings.All.FfmpegLocation = "ffmpeg.exe";
                return true;
            }

            //Program Data folder.
            var expandedPath = Environment.ExpandEnvironmentVariables(@"%ProgramData%\ScreenToGif\ffmpeg.exe");

            if (File.Exists(expandedPath))
            {
                UserSettings.All.FfmpegLocation = expandedPath;
                return true;
            }
        }

        //If not found by direct/relative path, ignore the environment variables.
        if (ignoreEnvironment)
            return false;

        #region Check Environment Variables

        var variable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) + ";" +
                       Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);

        foreach (var path in variable.Split(';').Where(w => !string.IsNullOrWhiteSpace(w)))
        {
            try
            {
                if (!File.Exists(Path.Combine(path, "ffmpeg.exe")))
                    continue;
            }
            catch (Exception)
            {
                //LogWriter.Log(ex, "Checking the path variables", path);
                continue;
            }

            UserSettings.All.FfmpegLocation = Path.Combine(path, "ffmpeg.exe");
            return true;
        }

        #endregion

        return false;
    }

    public static bool IsGifskiPresent(bool ignoreEnvironment = false, bool ignoreEmpty = false)
    {
        //If the path is relative, File.Exists() was returning C:\\Windows\\System32\Gifski.dll when the app was launched from the "Open with" context menu.
        //So, in order to get the correct location, I need to combine the current base directory with the relative path.
        var realPath = AdjustPath(UserSettings.All.GifskiLocation);

        //File location already chosen or detected.
        if (!string.IsNullOrWhiteSpace(realPath) && File.Exists(realPath))
            return true;

        //The path was not selected, but the file exists inside the same folder.
        if (!ignoreEmpty && string.IsNullOrWhiteSpace(UserSettings.All.GifskiLocation) && File.Exists(AdjustPath("gifski.dll")))
        {
            UserSettings.All.GifskiLocation = "gifski.dll";
            return true;
        }

        //If not found by direct/relative path, ignore the environment variables.
        if (ignoreEnvironment)
            return false;

        #region Check Environment Variables

        var variable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) + ";" +
                       Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);

        foreach (var path in variable.Split(';').Where(w => !string.IsNullOrWhiteSpace(w)))
        {
            try
            {
                if (!File.Exists(Path.Combine(path, "gifski.dll")))
                    continue;
            }
            catch (Exception ex)
            {
                //LogWriter.Log(ex, "Checking the path variables", path);
                continue;
            }

            UserSettings.All.GifskiLocation = Path.Combine(path, "gifski.dll");
            return true;
        }

        #endregion

        return false;
    }

    #endregion
}