using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace ScreenToGif.Util.InterProcessChannel;

public class PipeServer<TMessage> : IDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly StreamReader _reader;
    private readonly SynchronizationContext _synchronizationContext;
    private readonly CancellationTokenSource _source;

    public event EventHandler<TMessage> MessageReceived;

    public PipeServer(string pipeName)
    {
        _source = new CancellationTokenSource();
        _pipe = new NamedPipeServerStream(pipeName, PipeDirection.In);
        _reader = new StreamReader(_pipe, Encoding.UTF8);
        _synchronizationContext = SynchronizationContext.Current;

        //Start the loop on the threadpool. This will run for the duration of the application, but the LongRunning option should not be used as the thread will not be used the whole time.
        _ = Task.Run(ServerLoop).ContinueWith(t => OnServerError(t.Exception), default, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void Stop()
    {
        _source.Cancel();
        _pipe.Disconnect();

        Dispose();
    }

    private async void ServerLoop()
    {
        while (true)
        {
            await _pipe.WaitForConnectionAsync(_source.Token);

            var message = await JsonSerializer.DeserializeAsync<TMessage>(_pipe, (JsonSerializerOptions) null, _source.Token);

            _pipe.Disconnect();

            if (message != null)
                _synchronizationContext.Post(OnMessageReceived, message);
        }
    }

    protected virtual void OnMessageReceived(object state)
    {
        MessageReceived?.Invoke(this, (TMessage)state);
    }

    protected virtual void OnServerError(AggregateException exception)
    {
        LogWriter.Log(exception, "IPC Server error.");
    }

    public void Dispose()
    {
        _pipe.Dispose();
        _reader.Dispose();
        _source.Dispose();
    }
}