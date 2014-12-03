using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
            //var rec = new Recorder();
            //rec.ShowDialog();
            //return;

            var startup = new Startup();
            startup.ShowDialog();

            Environment.Exit(0);
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            //TODO: Save all settings, stop all encoding.
        }
    }
}
