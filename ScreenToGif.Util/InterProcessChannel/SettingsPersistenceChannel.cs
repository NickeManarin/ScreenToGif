using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace ScreenToGif.Util.InterProcessChannel;

internal struct SettingsPersistenceMessage
{
    /// <summary>
    /// The serialized dictionary to be passed to the other instance.
    /// </summary>
    public string Serialized { get; set; }

    /// <summary>
    /// True if the settings is local.
    /// </summary>
    public bool IsLocal { get; set; }
}

/// <summary>
/// Interprocess channel that is responsible for passing to another instance (with administrative rights) in order to be saved.
/// </summary>
public static class SettingsPersistenceChannel
{
    private const string PipeName = "ScreenToGit.SettingsPersistence";

    private static PipeServer<SettingsPersistenceMessage> _server;


    public static void RegisterServer()
    {
        try
        {
            if (_server != null)
                return;

            using (var process = Process.GetCurrentProcess())
                _server = new PipeServer<SettingsPersistenceMessage>(PipeName + process.Id);

            _server.MessageReceived += ServerOnMessageReceived;
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
            _server.MessageReceived -= ServerOnMessageReceived;
            _server = null;
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "It was not possible to unregister the IPC server.");
        }
    }

    public static void SendMessage(int processId, string serialized, bool isLocal)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName + processId, PipeDirection.Out);
            pipe.Connect();

            var message = new SettingsPersistenceMessage
            {
                Serialized = serialized,
                IsLocal = isLocal
            };
            var buffer = JsonSerializer.SerializeToUtf8Bytes(message);

            pipe.Write(buffer, 0, buffer.Length);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "It was not possible to send a message via the IPC server.");
        }
    }

    private static void ServerOnMessageReceived(object sender, SettingsPersistenceMessage message)
    {
        try
        {
            Settings.UserSettings.All.SaveFromAnotherInstance(message.Serialized, message.IsLocal);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Unable to execute arguments from IPC.");
        }
        finally
        {
            UnregisterServer();
        }
    }
}