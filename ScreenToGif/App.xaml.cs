using System;
using System.Windows;
using System.Windows.Threading;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;
using ScreenToGif.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /*
         * Startup scheme:
         * Startup > Recorder > Editor
         * Startup > Editor
         * Recorder > Editor
         */

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            #region Unhandled Exceptions

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            #endregion

            #region Arguments

            try
            {
                if (e.Args.Length > 0)
                {
                    //TODO: Watch for Args...                
                }
            }
            catch (Exception ex)
            {
                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
                LogWriter.Log(ex, "Generic Exception - Arguments");
            }

            #endregion

            try
            {
                #region Startup

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
                        }

                        #endregion
                    }
                }
                else
                {
                    var edit = new Editor();
                    edit.ShowDialog();
                }

                #endregion
            }
            catch (Exception ex)
            {
                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
                LogWriter.Log(ex, "Generic Exception - Root");
            }
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            //TODO: Save all settings, stop all encoding.
        }

        #region Exception Handling

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var errorViewer = new ExceptionViewer(e.Exception);
            errorViewer.ShowDialog();
            LogWriter.Log(e.Exception, "On Dispacher Unhandled Exception - Unknow");

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception == null) return;

            var errorViewer = new ExceptionViewer(exception);
            errorViewer.ShowDialog();
            LogWriter.Log(exception, "Current Domain Unhandled Exception - Unknow");
        }

        #endregion
    }
}
