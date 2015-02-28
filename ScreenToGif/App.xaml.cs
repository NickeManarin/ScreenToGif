using System;
using System.Windows;
using ScreenToGif.Properties;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;
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

            try
            {
                if (Settings.Default.StartUp == 0)
                {
                    var startup = new Startup();
                    startup.ShowDialog();
                }
                else if (Settings.Default.StartUp == 1)
                {
                    var rec = new Recorder(true);

                    var result = rec.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        #region If Close

                        Environment.Exit(0);

                        #endregion
                    }
                    else if (result.HasValue)
                    {
                        #region If Backbutton or Stop Clicked

                        if (rec.ExitArg == ExitAction.Recorded)
                        {
                            var editor = new Editor {ListFrames = rec.ListFrames};
                            editor.ShowDialog();
                            return;
                        }

                        #endregion
                    }
                }
                else
                {
                    var edit = new Editor();
                    edit.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
                LogWriter.Log(ex, "NullPointer in the Stop function");
            }
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            //TODO: Save all settings, stop all encoding.
        }
    }
}
