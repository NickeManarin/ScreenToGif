using System.Windows.Threading;

namespace ScreenToGif.Util;

public class DebounceDispatcher
{
    private DispatcherTimer _timer;

    public DateTime SyncTime { get; set; } = DateTime.Now;

    public DateTime TimerStarted { get; private set; } = DateTime.Now.AddYears(-1);

    /// <summary>
    /// Debounce an event by resetting the event timeout every time the event is 
    /// fired. The behavior is that the Action passed is fired only after events
    /// stop firing for the given timeout period.
    /// 
    /// Use Debounce when you want events to fire only after events stop firing
    /// after the given interval timeout period.
    /// 
    /// Wrap the logic you would normally use in your event code into
    /// the  Action you pass to this method to debounce the event.
    /// Example: https://gist.github.com/RickStrahl/0519b678f3294e27891f4d4f0608519a
    /// </summary>
    /// <param name="interval">Timeout in Milliseconds</param>
    /// <param name="action">Action<object> to fire when debounced event fires</object></param>
    /// <param name="param">optional parameter</param>
    /// <param name="priority">optional priority for the dispatcher</param>
    /// <param name="dispatcher">optional dispatcher. If not passed or null CurrentDispatcher is used.</param>        
    public void Debounce(int interval, Action<object> action, object param = null, DispatcherPriority priority = DispatcherPriority.ApplicationIdle, Dispatcher dispatcher = null)
    {
        //Kill pending timer and pending ticks.
        _timer?.Stop();
        _timer = null;

        dispatcher ??= Dispatcher.CurrentDispatcher;

        //Timer is recreated for each event and effectively resets the timeout.
        //Action only fires after timeout has fully elapsed without other events firing in between.
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, (s, e) =>
        {
            if (_timer == null)
                return;

            _timer?.Stop();
            _timer = null;
            action.Invoke(param);
        }, dispatcher);

        _timer.Start();
        TimerStarted = DateTime.Now;
    }

    public void Debounce(int interval, Task task, CancellationToken token, DispatcherPriority priority = DispatcherPriority.ApplicationIdle, Dispatcher dispatcher = null)
    {
        //Kill pending timer and pending ticks.
        _timer?.Stop();
        _timer = null;

        dispatcher ??= Dispatcher.CurrentDispatcher;

        //Timer is recreated for each event and effectively resets the timeout.
        //Action only fires after timeout has fully elapsed without other events firing in between.
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, async (s, e) =>
        {
            if (_timer == null)
                return;

            _timer?.Stop();
            _timer = null;

            await task.WaitAsync(token);
        }, dispatcher);

        _timer.Start();
        TimerStarted = DateTime.Now;
    }

    /// <summary>
    /// This method throttles events by allowing only 1 event to fire for the given
    /// timeout period. Only the last event fired is handled - all others are ignored.
    /// Throttle will fire events every timeout ms even if additional events are pending.
    /// 
    /// Use Throttle where you need to ensure that events fire at given intervals.
    /// </summary>
    /// <param name="interval">Timeout in Milliseconds</param>
    /// <param name="action">Action<object> to fire when debounced event fires</object></param>
    /// <param name="param">optional parameter</param>
    /// <param name="priority">optional priority for the dispatcher</param>
    /// <param name="dispatcher">optional dispatcher. If not passed or null CurrentDispatcher is used.</param>
    public void Throttle(int interval, Action<object> action, object param = null, DispatcherPriority priority = DispatcherPriority.ApplicationIdle, Dispatcher dispatcher = null)
    {
        //Kill pending timer and pending ticks.
        _timer?.Stop();
        _timer = null;

        dispatcher ??= Dispatcher.CurrentDispatcher;

        var curTime = DateTime.Now;

        //If timeout is not up yet - adjust timeout to fire with potentially new Action parameters.
        if (curTime.Subtract(TimerStarted).TotalMilliseconds < interval)
            interval -= (int) curTime.Subtract(TimerStarted).TotalMilliseconds;

        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, (s, e) =>
        {
            if (_timer == null)
                return;

            _timer?.Stop();
            _timer = null;
            action.Invoke(param);
        }, dispatcher);

        _timer.Start();
        TimerStarted = curTime;            
    }

    public void Cancel()
    {
        //Kill pending timer and pending ticks.
        _timer?.Stop();
        _timer = null;
    }
}
