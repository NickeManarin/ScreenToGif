using System.Windows;
using ScreenToGif.ViewModel;

namespace ScreenToGif.Windows
{
    public partial class EditorEx : Window
    {
        private readonly EditorViewModel _editorViewModel; 

        public EditorEx()
        {
            InitializeComponent();

            DataContext = _editorViewModel = new EditorViewModel();
        }

        private void Editor_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}