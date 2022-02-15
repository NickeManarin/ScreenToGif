using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ScreenToGif.Util;

public static class ProcessHelper
{
    public static string GetEntryAssemblyPath()
    {
        try
        {
            return Process.GetCurrentProcess().MainModule?.FileName ??
                System.Reflection.Assembly.GetEntryAssembly()?.GetName().CodeBase?.Replace("/ScreenToGif.dll", "/ScreenToGif.exe") ??
                Path.Combine(AppContext.BaseDirectory, "ScreenToGif.exe");
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Not possible to get current executing assembly path.");
            return Path.Combine(AppContext.BaseDirectory, "ScreenToGif.exe");
        }
    }

    public static async Task<string> Start(string arguments, bool runWithPowershell = true)
    {
        var info = new ProcessStartInfo(runWithPowershell ? "Powershell.exe" : "cmd.exe")
        {
            Arguments = (!runWithPowershell ? "/c " : "") + arguments,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process();
            process.StartInfo = info;
            process.Start();

            var message = await process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();

            return message;
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "It was not possible to run the command");
            return "";
        }
    }

    public static void StartWithShell(string filename)
    {
        var info = new ProcessStartInfo
        {
            FileName = filename,
            UseShellExecute = true
        };

        Process.Start(info);
    }
    
    public static async Task<bool> RestartAsAdmin(string arguments = "", bool waitToClose = false)
    {
        try
        {
            var info = new ProcessStartInfo(GetEntryAssemblyPath())
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

    public static Process RestartAsAdminAdvanced(string arguments = "")
    {
        try
        {
            var info = new ProcessStartInfo(GetEntryAssemblyPath())
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = arguments
            };

            return Process.Start(info);
        }
        catch (Win32Exception ex)
        {
            if (ex.NativeErrorCode != 1223) //User cancelled.
                LogWriter.Log(ex, "Impossible to start process as admin.");

            return null;
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to start process as admin.");
            return null;
        }
    }
}