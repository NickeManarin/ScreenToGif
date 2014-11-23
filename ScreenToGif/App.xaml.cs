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
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            /*
             * Startup scheme:
             * Startup > Recorder > Editor
             * Startup > Editor
             * Recorder > Editor
             * Just the Recorder (not sure if the editor will host the encoding process)
             */

            //var rec = new Recorder();
            //rec.ShowDialog();

            //return;

            var startup = new Startup();
            startup.ShowDialog();

            Environment.Exit(0);
        }
    }
}
