using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using ScreenToGif.Settings;

namespace ScreenToGif.Util.InterProcessChannel
{
    internal struct InstanceSwitcherMessage
    {
        public string[] Args { get; set; }
    }

    /// <summary>
    /// Interprocess channel that is responsible for passing to another instance the parameters of this instance (in cases of when just one instance is allowed).
    /// </summary>
    public static class InstanceSwitcherChannel
    {
        private const string PipeName = "ScreenToGit.InstanceSwitcher";

        private static PipeServer<InstanceSwitcherMessage> _server;


        internal static void RegisterServer()
        {
            try
            {
                if (_server != null)
                    return;

                using (var process = Process.GetCurrentProcess())
                    _server = new PipeServer<InstanceSwitcherMessage>(PipeName + process.Id);
                
                _server.MessageReceived += ServerOnMessageReceived;
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to register the IPC server.");
            }
        }

        internal static void UnregisterServer()
        {
            try
            {
                _server.Stop();
                _server.MessageReceived -= ServerOnMessageReceived;
                _server = null;
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to unregister the IPC server.");
            }
        }

        internal static void SendMessage(int processId, string[] args)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", PipeName + processId, PipeDirection.Out);
                pipe.Connect();

                var message = new InstanceSwitcherMessage { Args = args };
                var buffer = JsonSerializer.SerializeToUtf8Bytes(message);

                pipe.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to send a message via the IPC server.");
            }
        }

        private static void ServerOnMessageReceived(object sender, InstanceSwitcherMessage message)
        {
            try
            {
                var args = message.Args;

                if (args?.Length > 0)
                    Arguments.Prepare(args);

                if (Arguments.Open) 
                    App.MainViewModel.Open.Execute(Arguments.WindownToOpen, true);
                else
                    App.MainViewModel.Open.Execute(UserSettings.All.StartUp);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Unable to execute arguments from IPC.");
            }
        }
    }
}