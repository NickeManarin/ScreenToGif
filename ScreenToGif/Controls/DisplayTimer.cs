using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ScreenToGif.Controls;

public class DisplayTimer : Control
{
    private DispatcherTimer _timer = null;
    private Stopwatch _watch = null;


    public static readonly DependencyPropertyKey ElapsedPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Elapsed), typeof(TimeSpan), typeof(DisplayTimer), new PropertyMetadata(TimeSpan.Zero));

    public static readonly DependencyProperty ElapsedProperty = ElapsedPropertyKey.DependencyProperty;

    public static readonly DependencyPropertyKey IsRunningPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsRunning), typeof(bool), typeof(DisplayTimer), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty IsRunningProperty = IsRunningPropertyKey.DependencyProperty;

    public static readonly DependencyPropertyKey IsPausedPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsPaused), typeof(bool), typeof(DisplayTimer), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty IsPausedProperty = IsPausedPropertyKey.DependencyProperty;

    public static readonly DependencyPropertyKey IsNegativePropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsNegative), typeof(bool), typeof(DisplayTimer), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty IsNegativeProperty = IsNegativePropertyKey.DependencyProperty;


    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(DisplayTimer), new PropertyMetadata(default(CornerRadius)));

    public static readonly DependencyProperty CapturedCountProperty = DependencyProperty.Register(nameof(CapturedCount), typeof(int), typeof(DisplayTimer), new PropertyMetadata(0));

    public static readonly DependencyProperty ManuallyCapturedCountProperty = DependencyProperty.Register(nameof(ManuallyCapturedCount), typeof(int), typeof(DisplayTimer), new PropertyMetadata(0));

    public static readonly DependencyProperty IsImpreciseCaptureProperty = DependencyProperty.Register(nameof(IsImpreciseCapture), typeof(bool), typeof(DisplayTimer), new PropertyMetadata(false));


    public TimeSpan Elapsed
    {
        get => (TimeSpan)GetValue(ElapsedProperty);
        protected set => SetValue(ElapsedPropertyKey, value);
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        protected set => SetValue(IsRunningPropertyKey, value);
    }

    public bool IsPaused
    {
        get => (bool)GetValue(IsPausedProperty);
        protected set => SetValue(IsPausedPropertyKey, value);
    }
        
    public bool IsNegative
    {
        get => (bool)GetValue(IsNegativeProperty);
        protected set => SetValue(IsNegativePropertyKey, value);
    }



    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public int CapturedCount
    {
        get => (int)GetValue(CapturedCountProperty);
        set => SetValue(CapturedCountProperty, value);
    }

    public int ManuallyCapturedCount
    {
        get => (int)GetValue(ManuallyCapturedCountProperty);
        set => SetValue(ManuallyCapturedCountProperty, value);
    }
        
    public bool IsImpreciseCapture
    {
        get => (bool)GetValue(IsImpreciseCaptureProperty);
        set => SetValue(IsImpreciseCaptureProperty, value);
    }


    static DisplayTimer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DisplayTimer), new FrameworkPropertyMetadata(typeof(DisplayTimer)));
    }

    ~DisplayTimer()
    {
        _timer?.Stop();
    }


    private void SyncElapsed()
    {
        Elapsed = _watch.Elapsed;
    }

    public void Start()
    {
        IsRunning = true;
        IsPaused = false;
        IsNegative = false;

        if (_timer != null)
        {
            _watch.Start();
            _timer.Start();
            return;
        }

        _watch = new Stopwatch();
        _timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 100), DispatcherPriority.Background, (sender, args) => { SyncElapsed(); }, Dispatcher.CurrentDispatcher);
        SyncElapsed();
        _watch.Start();
        _timer.Start();
    }

    public void Pause()
    {
        if (!IsRunning)
            return;

        _watch.Stop();
        _timer.Stop();
        IsRunning = false;
        IsPaused = true;
    }

    public void Stop()
    {
        _watch?.Stop();
        _timer?.Stop();

        _watch = null;
        _timer = null;

        ManuallyCapturedCount = 0;
        Elapsed = TimeSpan.Zero;
        IsRunning = false;
        IsPaused = false;
    }

    public void Reset()
    {
        _watch.Stop();
        _timer?.Stop();
        IsRunning = false;
        IsPaused = false;
        ManuallyCapturedCount = 0;
        Elapsed = TimeSpan.Zero;

        Start();
    }

    public void SetElapsed(int seconds)
    {
        if (IsRunning)
            return;

        Elapsed = new TimeSpan(0, 0, 0, seconds);
        IsNegative = Elapsed < TimeSpan.Zero;
    }
}