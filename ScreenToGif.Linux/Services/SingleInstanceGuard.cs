namespace ScreenToGif.Linux.Services;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;

    private SingleInstanceGuard(Mutex mutex)
    {
        _mutex = mutex;
    }

    public static SingleInstanceGuard? TryAcquire(bool enabled)
    {
        if (!enabled)
            return new SingleInstanceGuard(new Mutex());

        var mutex = new Mutex(initiallyOwned: true, "ScreenToGif.Linux.SingleInstance", out var createdNew);
        if (createdNew)
            return new SingleInstanceGuard(mutex);

        mutex.Dispose();
        return null;
    }

    public void Dispose()
    {
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // An unowned mutex is used when multiple instances are allowed.
        }

        _mutex.Dispose();
    }
}
