using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ScreenToGif.Controls;

namespace ScreenToGif.Windows.Other
{
    public partial class TestField : Window
    {
        private IntPtr _handle;

        public TestField()
        {
            InitializeComponent();
        }

        private void WindowTest_OnLoaded(object sender, RoutedEventArgs e)
        {
            _handle = new WindowInteropHelper(this).Handle;

            //var adornerLayer = AdornerLayer.GetAdornerLayer(TestTriangle);
            //var adorner = new ElementAdorner(TestTriangle, true, true, true, MainCanvas, OnChange);
            //adorner.LayoutUpdated += (o, args) => Title = adorner.Width + " " + adorner.Height;
            //adornerLayer.Add(adorner);
        }

        private void OnChange()
        {

        }

        private void WindowTest_OnLocationChanged(object sender, EventArgs e)
        {
            //Native.Rect rect;
            //Native.GetWindowRect(_handle, out rect);

            //LeftLabel2.Content = rect.Left;
            //TopLabel2.Content = rect.Top;
            //RightLabel2.Content = rect.Right - rect.Left;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    ExamplePath.Data = Geometry.Parse(InputTextBox.Text);
            //}
            //catch (Exception ex)
            //{
            //    LogWriter.Log(ex, "Geometry Parse error", InputTextBox.Text);
            //}
        }

        private void Print_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Background = Brushes.Azure;
        }

        private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Background = Brushes.Aquamarine;
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Background = Brushes.DarkCyan;
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            //StatusList.Info("Hello!");
            //StatusList.Warning("I'm!");
            //StatusList.Error("Nicke!");
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            MainDrawingCanvas.DrawingMode = DrawingCanvas.DrawingModes.Select;
        }

        private void Shape_Click(object sender, RoutedEventArgs e)
        {
            MainDrawingCanvas.DrawingMode = DrawingCanvas.DrawingModes.Shape;
        }

        private void Ink_Click(object sender, RoutedEventArgs e)
        {
            MainDrawingCanvas.DrawingMode = DrawingCanvas.DrawingModes.Ink;
        }

        private void Thickness_Click(object sender, RoutedEventArgs e)
        {
            MainDrawingCanvas.StrokeThickness = MainDrawingCanvas.StrokeThickness == 5 ? 8 : 5;
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            MainDrawingCanvas.Stroke = MainDrawingCanvas.Stroke == Brushes.Black ? Brushes.DarkBlue : Brushes.Black;
            MainDrawingCanvas.Fill = MainDrawingCanvas.Fill == Brushes.Transparent ? Brushes.LightBlue : Brushes.Transparent;
        }

        private void DashArray_Click(object sender, RoutedEventArgs e)
        {
            MainDrawingCanvas.StrokeDashArray = MainDrawingCanvas.StrokeDashArray.Count == 0 ? new DoubleCollection() { 5 } : new DoubleCollection();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }
    }
}