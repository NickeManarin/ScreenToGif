using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ScreenToGif.Util
{
    public static class VisualHelper
    {
        public static readonly object LockObject = new object();

        public static TP GetParent<TP>(DependencyObject child, int i) where TP : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            var logicalParent = LogicalTreeHelper.GetParent(child);

            if (logicalParent is TP)
                return logicalParent as TP;

            if (i > 4 || parent == null || parent is TP)
                return parent as TP;

            return GetParent<TP>(parent, i + 1);
        }

        public static T GetVisualParent<T>(this DependencyObject child) where T : Visual
        {
            while (child != null && !(child is T))
                child = VisualTreeHelper.GetParent(child);

            return child as T;
        }

        public static TP GetParent<TP>(DependencyObject child, Type stopWhen) where TP : Visual
        {
            var parent = VisualTreeHelper.GetParent(child);
            var logicalParent = LogicalTreeHelper.GetParent(child);

            if (logicalParent is TP correctLogical)
                return correctLogical;

            if (parent is TP correctParent)
                return correctParent;

            if (parent == null || parent.GetType() == stopWhen)
                return null;

            return GetParent<TP>(parent, stopWhen);
        }

        public static bool HasParent<T>(DependencyObject child, Type stopWhen, bool checkSelf = false) where T : Visual
        {
            if (checkSelf && child is T)
                return true;

            var parent = VisualTreeHelper.GetParent(child);
            var logicalParent = LogicalTreeHelper.GetParent(child);

            if (logicalParent is T)
                return true;

            if (parent is T)
                return true;

            if (parent == null || parent.GetType() == stopWhen)
                return false;

            return HasParent<T>(parent, stopWhen);
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            var child = default(T);
            var numVisuals = VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < numVisuals; i++)
            {
                var v = (Visual)VisualTreeHelper.GetChild(parent, i);

                child = v as T ?? GetVisualChild<T>(v);

                if (child != null)
                    break;
            }

            return child;
        }

        public static T DeepCopy<T>(UIElement source) where T : new()
        {
            if (source == null)
                return new T();

            var savedObject = System.Windows.Markup.XamlWriter.Save(source);

            var stringReader = new StringReader(savedObject);
            var xmlReader = System.Xml.XmlReader.Create(stringReader);

            return (T)System.Windows.Markup.XamlReader.Load(xmlReader);
        }

        public static Storyboard FindStoryboard(this FrameworkElement visual, string key)
        {
            var resource = visual.TryFindResource(key) as Storyboard;

            if (resource == null)
                return new Storyboard();

            return resource;
        }

        internal static bool IsInDesignMode()
        {
            return (bool) DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty,
                typeof(FrameworkElement)).Metadata.DefaultValue;
        }

        /// <summary>
        /// Checks if the <see cref="FrameworkElement.DataContextProperty"/> is bound or not.
        /// </summary>
        /// <param name="element">The element to be checked.</param>
        /// <returns>True if the data context property is being managed by a binding expression.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="element"/> is a null reference.</exception>
        internal static bool IsDataContextDataBound(this FrameworkElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return element.GetBindingExpression(FrameworkElement.DataContextProperty) != null;
        }

        /// <summary>
        /// Updates the taskbar icons.
        /// </summary>
        /// <param name="data">Configuration settings for the NotifyIcon.</param>
        /// <param name="command">Operation on the icon (e.g. delete the icon).</param>
        /// <param name="flags">Defines which members of the <paramref name="data"/> structure are set.</param>
        /// <returns>True if the data was successfully written.</returns>
        /// <remarks>See Shell_NotifyIcon documentation on MSDN for details.</remarks>
        internal static bool WriteIconData(ref Native.NotifyIconData data, Native.NotifyCommand command, Native.IconDataMembers flags)
        {
            if (IsInDesignMode())
                return true;

            data.ValidMembers = flags;

            lock (LockObject)
                return Native.Shell_NotifyIcon(command, ref data);
        }

        private static readonly Action EmptyDelegate = delegate { };

        public static void Refresh(this UIElement uiElement)
        {
            uiElement?.Dispatcher?.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }
}