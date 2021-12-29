using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Translator.Util;

namespace Translator;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        //Unhandled Exceptions.
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogWriter.Log(e.Exception, "On Dispacher Unhandled Exception - Unknown");

        try
        {
            ExceptionDialog.Ok(e.Exception, "ScreenToGif - Translator", "Unhandled exception", e.Exception.Message);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while displaying the error.");
            //Ignored.
        }

        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception)
            return;

        LogWriter.Log(exception, "Current Domain Unhandled Exception - Unknown");

        try
        {
            ExceptionDialog.Ok(exception, "ScreenToGif - Translator", "Unhandled exception", exception.Message);
        }
        catch (Exception)
        {
            //Ignored.
        }
    }

    public static string Version => ToStringShort(Assembly.GetEntryAssembly()?.GetName().Version) ?? "0.0";

    internal static string ToStringShort(Version version)
    {
        if (version == null)
            return null;

        var result = $"{version.Major}.{version.Minor}";

        if (version.Build > 0)
            result += $".{version.Build}";

        if (version.Revision > 0)
            result += $".{version.Revision}";

        return result;
    }
}