using System.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Util
{
    public class WindowsManager
    {
        public static WindowsManager Default = new WindowsManager();

        public void ShowMainWindow()
        {
            var currentMainWindow = Application.Current.MainWindow;
            if (currentMainWindow == null)
            {
                var startup = new Startup();
                Application.Current.MainWindow = startup;
                startup.ShowDialog();
            }
            else
            {
                if (currentMainWindow?.WindowState == WindowState.Minimized)
                    currentMainWindow.WindowState = WindowState.Normal;
                currentMainWindow.Activate();
            }
        }
    }
}