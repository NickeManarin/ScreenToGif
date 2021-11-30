using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace ScreenToGif.Util.InterProcessChannel;

public struct InstanceSwitcherMessage
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
    private static Action<object, InstanceSwitcherMessage> _receivedAction;


    public static void RegisterServer(Action<object, InstanceSwitcherMessage> receivedAction)
    {
        try
        {
            if (_server != null)
                return;

            using (var process = Process.GetCurrentProcess())
                _server = new PipeServer<InstanceSwitcherMessage>(PipeName + process.Id);

            _receivedAction = receivedAction;
            _server.MessageReceived += receivedAction.Invoke;
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "It was not possible to register the IPC server.");
        }
    }

    public static void UnregisterServer()
    {
        try
        {
            _server.Stop();
            _server.MessageReceived -= _receivedAction.Invoke;
            _server = null;
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "It was not possible to unregister the IPC server.");
        }
    }

    public static void SendMessage(int processId, string[] args)
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

    //Maybe parametrize to be filled in by the main project.
    //private static void ServerOnMessageReceived(object sender, InstanceSwitcherMessage message)
    //{
    //    try
    //    {
    //        var args = message.Args;

    //        if (args?.Length > 0)
    //            Arguments.Prepare(args);

    //        if (Arguments.Open) 
    //            App.MainViewModel.Open.Execute(Arguments.WindownToOpen, true);
    //        else
    //            App.MainViewModel.Open.Execute(UserSettings.All.StartUp);
    //    }
    //    catch (Exception e)
    //    {
    //        LogWriter.Log(e, "Unable to execute arguments from IPC.");
    //    }
    //}
}