using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ScreenToGif.Util
{
    internal static class ProcessHelper
    {
        internal static async Task<bool> RestartAsAdmin(string arguments = "", bool waitToClose = false)
        {
            try
            {
                var fileName = Process.GetCurrentProcess().MainModule?.FileName ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;

                var info = new ProcessStartInfo(fileName)
                {
                    UseShellExecute = true, 
                    Verb = "runas", 
                    Arguments = arguments
                };
                
                var process = Process.Start(info);

                if (waitToClose && process != null)
                {
                    var comp = new TaskCompletionSource<bool>();

                    process.Exited += (sender, args) =>
                    {
                        comp.SetResult(process.ExitCode != 90);
                    };
                    process.EnableRaisingEvents = true;

                    if (process.HasExited)
                        return process.ExitCode != 90;

                    //Return only when the region gets selected.
                    return await comp.Task;
                }

                return true;
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode != 1223) //User cancelled.
                    LogWriter.Log(ex, "Impossible to start process as admin.");
                
                return false;
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to start process as admin.");
                return false;
            }
        }
    }
}