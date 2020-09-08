using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    public class StatusList : StackPanel
    {
        #region Dependency Properties/Events

        public static readonly DependencyProperty MaxBandsProperty = DependencyProperty.Register("MaxBands", typeof(int), typeof(StatusBand),
            new FrameworkPropertyMetadata(5));

        #endregion

        #region Properties

        [Bindable(true), Category("Common")]
        public int MaxBands
        {
            get => (int)GetValue(MaxBandsProperty);
            set => SetValue(MaxBandsProperty, value);
        }

        #endregion

        private void Add(StatusType type, string text, Action action = null)
        {
            var current = Children.OfType<StatusBand>().FirstOrDefault(x => x.Type == type && x.Text == text);

            if (current != null)
                Children.Remove(current);

            var band = new StatusBand();
            band.Dismissed += (sender, args) => Children.Remove(band);

            if (Children.Count >= MaxBands)
                Children.RemoveAt(0);

            Children.Add(band);

            switch (type)
            {
                case StatusType.Info:
                    band.Info(text, action);
                    break;
                case StatusType.Warning:
                    band.Warning(text, action);
                    break;
                case StatusType.Error:
                    band.Error(text, action);
                    break;
            }
        }

        public void Info(string text, Action action = null)
        {
            Add(StatusType.Info, text, action);
        }

        public void Warning(string text, Action action = null)
        {
            Add(StatusType.Warning, text, action);
        }

        public void Error(string text, Action action = null)
        {
            Add(StatusType.Error, text, action);
        }

        public void Remove(StatusType type)
        {
            var list = Children.OfType<StatusBand>().Where(x => x.Type == type).ToList();

            foreach (var band in list)
                Children.Remove(band);
        }

        public void Clear()
        {
            Children.Clear();
        }
    }
}