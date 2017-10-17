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
using ScreenToGif.FileWriters;
using ScreenToGif.Util.Model;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Other helper methods.
    /// </summary>
    public static class Other
    {
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

            var pointClientPixels = new Native.PointW();
            pointClientPixels.X = (0 < pointClient.X) ? (int)(pointClient.X + 0.5) : (int)(pointClient.X - 0.5);
            pointClientPixels.Y = (0 < pointClient.Y) ? (int)(pointClient.Y + 0.5) : (int)(pointClient.Y - 0.5);

            // Transform the point into screen coordinates.
            var pointScreenPixels = pointClientPixels;
            Native.ClientToScreen(hwndSource.Handle, ref pointScreenPixels);

            //Native.GetCurrentPositionEx(hwndSource.Handle, out pointScreenPixels);
            //Native.GetWindowOrgEx(hwndSource.Handle, out pointScreenPixels);

            return new Point(pointScreenPixels.X, pointScreenPixels.Y);
        }

        public static bool IsWin8OrHigher()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version < new Version(6, 2, 9200, 0))
                return false;

            //This version only uses white chromes.
            if (Environment.OSVersion.Version == new Version(10, 0, 10240, 0))
                return false;

            if (Environment.OSVersion.Version > new Version(10, 0, 10240, 0) && !Glass.UsesColor)
                return false;

            return true;
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

        /// <summary>
        /// The Greater Common Divisor.
        /// </summary>
        /// <param name="a">Size a</param>
        /// <param name="b">Size b</param>
        /// <returns>The GCD number.</returns>
        public static double Gcd(double a, double b)
        {
            return b == 0 ? a : Gcd(b, a % b);
        }

        public static int DivisibleByTwo(this int number)
        {
            return number % 2 == 0 ? number : number + 1;
        }

        private static Size MeasureString(this TextBlock textBlock)
        {
            var formattedText = new FormattedText(textBlock.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch), textBlock.FontSize, Brushes.Black);

            return new Size(formattedText.Width, formattedText.Height);
        }

        internal static Rect Offset(this Rect rect, double offset)
        {
            return new Rect(Math.Round(rect.Left + offset, MidpointRounding.AwayFromZero), Math.Round(rect.Top + offset, MidpointRounding.AwayFromZero),
                Math.Round(rect.Width - (offset * 2d), MidpointRounding.AwayFromZero), Math.Round(rect.Height - (offset * 2d), MidpointRounding.AwayFromZero));

            //return new Rect(rect.Left + offset, rect.Top + offset, rect.Width - (offset * 2d), rect.Height - (offset * 2d));
        }

        internal static Rect Scale(this Rect rect, double scale)
        {
            return new Rect(Math.Round(rect.Left * scale, MidpointRounding.AwayFromZero), Math.Round(rect.Top * scale, MidpointRounding.AwayFromZero),
                Math.Round(rect.Width * scale, MidpointRounding.AwayFromZero), Math.Round(rect.Height * scale, MidpointRounding.AwayFromZero));
        }

        internal static Size Scale(this Size size, double scale)
        {
            return new Size(Math.Round(size.Width * scale, MidpointRounding.AwayFromZero), Math.Round(size.Height * scale, MidpointRounding.AwayFromZero));
        }

        internal static Point Scale(this Point point, double scale)
        {
            return new Point(Math.Round(point.X * scale, MidpointRounding.AwayFromZero), Math.Round(point.Y * scale, MidpointRounding.AwayFromZero));
        }

        public static double RoundUpValue(double value, int decimalpoint = 0)
        {
            var result = Math.Round(value, decimalpoint);

            if (result < value)
                result += Math.Pow(10, -decimalpoint);

            return result;
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

        public static string Remove(this string text, params string[] keys)
        {
            if (text == null)
                throw new ArgumentNullException("text", "The text should not be null.");

            foreach (var key in keys)
                text = text.Replace(key, string.Empty);

            return text;
        }

        public static bool Contains(this Int32Rect first, Int32Rect second)
        {
            if (first.IsEmpty || second.IsEmpty || (first.X > second.X || first.Y > second.Y) || first.X + first.Width < second.X + second.Width)
                return false;

            return first.Y + first.Height >= second.Y + second.Height;
        }

        public static List<DetectedRegion> AdjustPosition(this List<DetectedRegion> list, double x, double y)
        {
            foreach (var region in list)
                region.Bounds = new Rect(new Point(region.Bounds.X - x, region.Bounds.Y - y), region.Bounds.Size);

            return list;
        }

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

        #region List

        public static List<FrameInfo> CopyList(this List<FrameInfo> target)
        {
            return new List<FrameInfo>(target.Select(item => new FrameInfo(item.Path, item.Delay,
                new List<SimpleKeyGesture>(item.KeyList.Select(y => new SimpleKeyGesture(y.Key, y.Modifiers, y.IsUppercase))))));
        }

        /// <summary>
        /// Creates an index list based on the start and end indexes (positions). 
        /// </summary>
        /// <param name="start">The start index.</param>
        /// <param name="end">The end index. If it's a lower value than the start index, the start becomes the end and vice-versa.</param>
        /// <returns>A list of ordered integers.</returns>
        public static List<int> CreateIndexList(int start, int end)
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
        public static List<int> CreateIndexList2(int start, int quantity)
        {
            //if (start > end)
            //    return Enumerable.Range(end, start - end + 1).ToList();

            return Enumerable.Range(start, quantity).ToList();
        }

        /// <summary>
        /// Copies the List and saves the images in another folder.
        /// </summary>
        /// <param name="target">The List to copy</param>
        /// <returns>The copied list.</returns>
        public static List<FrameInfo> CopyToEncode(this List<FrameInfo> target)
        {
            #region Folder

            var fileNameAux = Path.GetFileName(target[0].Path);

            if (fileNameAux == null)
                throw new ArgumentException("Impossible to get filename.");

            var encodeFolder = Path.Combine(target[0].Path.Replace(fileNameAux, ""), "Encode " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss"));

            if (!Directory.Exists(encodeFolder))
                Directory.CreateDirectory(encodeFolder);

            #endregion

            var newList = new List<FrameInfo>();

            try
            {
                foreach (var frameInfo in target)
                {
                    //Changes the path of the image. Writes as an ordered list of files, replacing the old filenames.
                    var filename = Path.Combine(encodeFolder, newList.Count + ".png");
                    //var filename = Path.Combine(encodeFolder, Path.GetFileName(frameInfo.Path) + ".png");

                    //Copy the image to the folder.
                    File.Copy(frameInfo.Path, filename, true);
                    //File.Copy(frameInfo.Path, filename);

                    //Create the new object and add to the list.
                    newList.Add(new FrameInfo(filename, frameInfo.Delay));
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "");
                throw;
            }

            return newList;
        }

        /// <summary>
        /// Makes a Yo-yo efect with the given List (List + Reverted List)
        /// </summary>
        /// <param name="list">The list to apply the efect</param>
        /// <returns>A List with the Yo-yo efect</returns>
        public static List<FrameInfo> Yoyo(List<FrameInfo> list)
        {
            var listReverted = new List<FrameInfo>(list);
            listReverted.Reverse();

            var currentFolder = Path.GetDirectoryName(list[0].Path);

            foreach (var frame in listReverted)
            {
                var newPath = Path.Combine(currentFolder, list.Count + " Y " + DateTime.Now.ToString("yy MM dd hh mm ss fff") + ".png");

                File.Copy(frame.Path, newPath);

                var newFrame = new FrameInfo(newPath, frame.Delay);

                list.Add(newFrame);
            }

            return list;
        }

        public static List<FrameInfo> Move(this List<FrameInfo> list, int oldIndex, int newIndex)
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

        public static bool IsFfmpegPresent()
        {
            //File location already choosen or detected.
            if (!string.IsNullOrWhiteSpace(UserSettings.All.FfmpegLocation) && File.Exists(UserSettings.All.FfmpegLocation))
                return true;

            #region Check Environment Variables

            var variable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) + ";" +
                Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);

            foreach (var path in variable.Split(';'))
            {
                try
                {
                    if (!File.Exists(Path.Combine(path, "ffmpeg.exe"))) continue;
                }
                catch (Exception ex)
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

        #endregion
    }
}