using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.Model;
using ScreenToGif.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif
{
    public partial class App
    {
        private static NotifyIcon _notifyIcon;

        #region Events

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Global.StartupDateTime = DateTime.Now;

            //Unhandled Exceptions.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //Increases the duration of the tooltip display.
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

            #region Arguments

            try
            {
                if (e.Args.Length > 0)
                    Argument.Prepare(e.Args);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Generic Exception - Arguments");

                ErrorDialog.Ok("ScreenToGif", "Generic error - arguments", ex.Message, ex);
            }

            #endregion

            #region Language

            try
            {
                LocalizationHelper.SelectCulture(UserSettings.All.LanguageCode);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Language Settings Exception.");

                ErrorDialog.Ok("ScreenToGif", "Generic error - language", ex.Message, ex);
            }

            #endregion

            #region Net Framework

            var array = Type.GetType("System.Array");
            var method = array?.GetMethod("Empty");

            if (array == null || method == null)
            {
                var ask = Dialog.Ask("Missing Dependency", "Net Framework 4.6.1 is not present", "In order to properly use this app, you need to download the correct version of the .Net Framework. Open the web page to download?");

                if (ask)
                {
                    Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=49981");
                    return;
                }
            }

            #endregion

            _notifyIcon = (NotifyIcon)FindResource("NotifyIcon");

            //var select = new SelectFolderDialog(); select.ShowDialog(); return;
            //var select = new TestField(); select.ShowDialog(); return;
            //var select = new Encoder(); select.ShowDialog(); return;

            try
            {
                #region Startup

                var vm = new ApplicationViewModel();

                if (UserSettings.All.StartUp == 4 || Argument.FileNames.Any())
                {
                    vm.OpenEditor.Execute(null);
                    return;
                }

                if (UserSettings.All.StartUp == 0)
                {
                    vm.OpenLauncher.Execute(null);
                    return;
                }

                if (UserSettings.All.StartUp == 1)
                {
                    vm.OpenRecorder.Execute(null);
                    return;
                }

                if (UserSettings.All.StartUp == 2)
                {
                    vm.OpenWebcamRecorder.Execute(null);
                    return;
                }

                if (UserSettings.All.StartUp == 3)
                    vm.OpenBoardRecorder.Execute(null);

                #endregion
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Generic Exception - Root");

                ErrorDialog.Ok("ScreenToGif", "Generic error", ex.Message, ex);
            }
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            _notifyIcon?.Dispose();

            UserSettings.Save();
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogWriter.Log(e.Exception, "On Dispacher Unhandled Exception - Unknown");

            try
            {
                ErrorDialog.Ok("ScreenToGif", "Generic error - unknown", e.Exception.Message, e.Exception);
            }
            catch (Exception)
            {
                //Ignored.
            }

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!(e.ExceptionObject is Exception exception)) return;

            LogWriter.Log(exception, "Current Domain Unhandled Exception - Unknown");

            try
            {
                ErrorDialog.Ok("ScreenToGif", "Generic error - unhandled", exception.Message, exception);
            }
            catch (Exception)
            {
                //Ignored.
            }
        }

        #endregion
    }
}