using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Controls;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Util.Capture
{
    internal static class RegionSelectHelper
    {
        #region Properties

        private static TaskCompletionSource<Rect> _taskCompletionSource;

        private static readonly List<RegionSelector> Selectors = new List<RegionSelector>();

        internal static bool IsSelecting => Selectors.Any(a => a.IsVisible && a.IsActive);

        #endregion

        internal static Task<Rect> Select(SelectControl.ModeType mode, Rect previousRegion)
        {
            _taskCompletionSource = new TaskCompletionSource<Rect>();

            Selectors.Clear();

            foreach (var monitor in Monitor.AllMonitorsGranular())
            {
                var selector = new RegionSelector();
                selector.Select(monitor, mode, previousRegion, RegionSelected, RegionChanged, RegionGotHover, RegionAborted);

                Selectors.Add(selector);
            }

            //Return only when the region gets selected.
            return _taskCompletionSource.Task;
        }

        internal static void Abort()
        {
            RegionAborted();
        }


        private static void RegionSelected(Rect region)
        {
            foreach (var selector in Selectors)
                selector.CancelSelection();

            _taskCompletionSource.SetResult(region);
        }

        private static void RegionChanged(Monitor monitor)
        {
            //When one monitor gets the focus, the other ones should be cleaned.
            foreach (var selector in Selectors.Where(w => w.Monitor.Handle != monitor.Handle))
                selector.ClearSelection();
        }

        private static void RegionGotHover(Monitor monitor)
        {
            //When one monitor gets the focus, the other ones should be cleaned.
            foreach (var selector in Selectors.Where(w => w.Monitor.Handle != monitor.Handle))
                selector.ClearHoverEffects();
        }

        private static void RegionAborted()
        {
            foreach (var selector in Selectors)
                selector.CancelSelection();

            _taskCompletionSource.SetResult(Rect.Empty);
        }
    }
}