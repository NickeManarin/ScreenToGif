using System.Windows;
using ScreenToGif.Properties;
using ScreenToGif.Windows;

namespace ScreenToGif
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /*
         * Startup scheme:
         * Startup > Recorder > Editor
         * Startup > Editor
         * Recorder > Editor
         */

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                //TODO: Watch for Args...                
            }

            if (Settings.Default.StartUp == 0)
            {
                var startup = new Startup();
                startup.ShowDialog();
            }
            else if (Settings.Default.StartUp == 1)
            {
                var rec = new Recorder(true);

                rec.ShowDialog();

                //TODO: Watch for the return...
            }
            else
            {
                var edit = new Editor();
                edit.ShowDialog();
            }
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            //TODO: Save all settings, stop all encoding.
        }
    }
}
