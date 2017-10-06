using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ScreenToGif.FileWriters;
using ScreenToGif.Util;
using ScreenToGif.Util.Model;
using ScreenToGif.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif
{
    public partial class App
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Global.StartupDateTime = DateTime.Now;

            #region Unhandled Exceptions

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            #endregion

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

            //try
            //{
            //    //If there's no Array.Empty, means that there's no .Net Framework 4.6.1
            //    //This is not the best way... 
            //    Array.Empty<int>();
            //}
            //catch (MissingMethodException ex)
            //{
            //    var ask = Dialog.Ask("Missing Dependency", "Net Framework 4.6.1 is not present", "In order to properly use this app, you need to download the correct version of the .Net Framework. Open the web page to download?");

            //    if (ask)
            //    {
            //        Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=49981");
            //        return;
            //    }

            //    LogWriter.Log(ex, "Missing .Net Framework 4.6.1");
            //}

            //if (Environment.Version.Build < 30319 && Environment.Version.Revision < 42000)
            //{
            //    var ask = Dialog.Ask("Missing Dependency", "Net Framework 4.6.1 is not present", "In order to properly use this app, you need to download the correct version of the .Net Framework. Open the page to download?");

            //    if (ask)
            //    {
            //        Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=49981");
            //        return;
            //    }
            //}

            #endregion

            //var select = new SelectFolderDialog();
            //var select = new TestField();
            //var select = new Encoder();
            //select.ShowDialog(); return;

            try
            {
                #region Startup

                if (UserSettings.All.StartUp == 0)
                {
                    var startup = new Startup();
                    Current.MainWindow = startup;
                    startup.ShowDialog();
                }
                else if (UserSettings.All.StartUp == 4 || Argument.FileNames.Any())
                {
                    var edit = new Editor();
                    Current.MainWindow = edit;
                    edit.ShowDialog();
                }
                else
                {
                    var editor = new Editor();
                    ProjectInfo project = null;
                    var exitArg = ExitAction.Exit;
                    bool? result = null;

                    #region Recorder, Webcam or Border

                    switch (UserSettings.All.StartUp)
                    {
                        case 1:
                            if (UserSettings.All.NewRecorder)
                            {
                                var recNew = new RecorderNew(true);
                                Current.MainWindow = recNew;

                                result = recNew.ShowDialog();
                                exitArg = recNew.ExitArg;
                                project = recNew.Project;
                                break;
                            }
                            
                            var rec = new Recorder(true);
                            Current.MainWindow = rec;

                            result = rec.ShowDialog();
                            exitArg = rec.ExitArg;
                            project = rec.Project;
                            break;
                        case 2:
                            var web = new Windows.Webcam(true);
                            Current.MainWindow = web;

                            result = web.ShowDialog();
                            exitArg = web.ExitArg;
                            project = web.Project;
                            break;
                        case 3:
                            var board = new Board();
                            Current.MainWindow = board;

                            result = board.ShowDialog();
                            exitArg = board.ExitArg;
                            project = board.Project;
                            break;
                    }

                    #endregion

                    if (result.HasValue && result.Value)
                    {
                        #region If Close

                        Environment.Exit(0);

                        #endregion
                    }
                    else if (result.HasValue)
                    {
                        #region If Backbutton or Stop Clicked

                        if (exitArg == ExitAction.Recorded)
                        {
                            editor.Project = project;
                            Current.MainWindow = editor;
                            editor.ShowDialog();
                        }

                        #endregion
                    }
                }

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
            UserSettings.Save();
        }

        #region Exception Handling

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogWriter.Log(e.Exception, "On Dispacher Unhandled Exception - Unknown");

            try
            {
                ErrorDialog.Ok("ScreenToGif", "Generic error - unknown", e.Exception.Message, e.Exception);
            }
            catch (Exception)
            { }

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception == null) return;

            LogWriter.Log(exception, "Current Domain Unhandled Exception - Unknown");

            try
            {
                ErrorDialog.Ok("ScreenToGif", "Generic error - unhandled", exception.Message, exception);
            }
            catch (Exception)
            { }
        }

        #endregion
    }
}