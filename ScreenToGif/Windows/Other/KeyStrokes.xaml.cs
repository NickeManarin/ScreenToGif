using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    /// <summary>
    /// Interaction logic for KeyStrokes.xaml
    /// </summary>
    public partial class KeyStrokes : Window
    {
        public List<FrameInfo> List { get; set; }

        public KeyStrokes()
        {
            InitializeComponent();
        }

        private void KeyStrokes_Loaded(object sender, RoutedEventArgs e)
        {
            if (List == null)
                return;

            ListBox.ItemsSource = List.SelectMany(x => x.KeyList)
                .Select(y => y.Modifiers == ModifierKeys.None ? y.Key.ToString() : y.Modifiers + " - " + y.Key);
        }
    }
}
