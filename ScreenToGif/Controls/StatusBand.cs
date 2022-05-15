using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using ScreenToGif.Domain.Enums;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;

namespace ScreenToGif.Controls;

public class StatusBand : Control
{
    #region Variables

    private Grid _warningGrid;
    private Button _suppressButton;

    #endregion

    #region Dependency Properties/Events

    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(int), typeof(StatusBand), new FrameworkPropertyMetadata(0));

    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(StatusType), typeof(StatusBand), new FrameworkPropertyMetadata(StatusType.None));

    public static readonly DependencyProperty ReasonProperty = DependencyProperty.Register(nameof(Reason), typeof(StatusReasons), typeof(StatusBand), new FrameworkPropertyMetadata(StatusReasons.None));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusBand));

    public static readonly DependencyProperty IsLinkProperty = DependencyProperty.Register(nameof(IsLink), typeof(bool), typeof(StatusBand), new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty StartingProperty = DependencyProperty.Register(nameof(Starting), typeof(bool), typeof(StatusBand), new PropertyMetadata(default(bool)));

    public static readonly RoutedEvent DismissedEvent = EventManager.RegisterRoutedEvent(nameof(Dismissed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(StatusBand));

    #endregion

    #region Properties

    [Bindable(true), Category("Common")]
    public int Id
    {
        get => (int)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    [Bindable(true), Category("Common")]
    public StatusType Type
    {
        get => (StatusType)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    [Bindable(true), Category("Common")]
    public StatusReasons Reason
    {
        get => (StatusReasons)GetValue(ReasonProperty);
        set => SetValue(ReasonProperty, value);
    }

    [Bindable(true), Category("Common")]
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    [Bindable(true), Category("Common")]
    public bool IsLink
    {
        get => (bool)GetValue(IsLinkProperty);
        set => SetValue(IsLinkProperty, value);
    }

    /// <summary>
    /// True if started to display the message.
    /// </summary>
    [Bindable(true), Category("Common")]
    public bool Starting
    {
        get => (bool)GetValue(StartingProperty);
        set => SetValue(StartingProperty, value);
    }

    /// <summary>
    /// Event raised when the StatusBand gets dismissed/suppressed.
    /// </summary>
    public event RoutedEventHandler Dismissed
    {
        add => AddHandler(DismissedEvent, value);
        remove => RemoveHandler(DismissedEvent, value);
    }

    public Action Action { get; set; }

    #endregion

    static StatusBand()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusBand), new FrameworkPropertyMetadata(typeof(StatusBand)));
    }

    public override void OnApplyTemplate()
    {
        _warningGrid = GetTemplateChild("WarningGrid") as Grid;
        var link = GetTemplateChild("MainHyperlink") as Hyperlink;
        _suppressButton = GetTemplateChild("SuppressButton") as ExtendedButton;

        if (_suppressButton != null)
            _suppressButton.Click += SuppressButton_Click;

        if (Action != null && link != null)
            link.Click += (sender, args) => Action.Invoke();

        base.OnApplyTemplate();
    }

    #region Methods

    public void Show(StatusType type, string text, Action action = null)
    {
        Action = action;

        //Collapsed-by-default elements do not apply templates.
        //http://stackoverflow.com/a/2115873/1735672
        //So it's necessary to do this here.
        ApplyTemplate();

        Starting = true;
        Type = type;
        Text = text;
        IsLink = action != null;

        if (_warningGrid?.FindResource("ShowWarningStoryboard") is Storyboard show)
            BeginStoryboard(show);
    }

    public void Update(string text, Action action = null)
    {
        Show(StatusType.Update, text, action);
    }

    public void Info(string text, Action action = null)
    {
        Show(StatusType.Info, text, action);
    }

    public void Warning(string text, Action action = null)
    {
        Show(StatusType.Warning, text, action);
    }

    public void Error(string text, Action action = null)
    {
        Show(StatusType.Error, text, action);
    }

    public void Hide()
    {
        Starting = false;

        if (_warningGrid?.Visibility == Visibility.Collapsed)
            return;

        if (_warningGrid?.FindResource("HideWarningStoryboard") is Storyboard hide)
            BeginStoryboard(hide);

        RaiseDismissedEvent();
    }

    public void RaiseDismissedEvent()
    {
        if (DismissedEvent == null || !IsLoaded)
            return;

        var newEventArgs = new RoutedEventArgs(DismissedEvent);
        RaiseEvent(newEventArgs);
    }

    public static string KindToString(StatusType kind)
    {
        return "Vector." + (kind == StatusType.None ? "Tag" : kind == StatusType.Info ? "Info" : kind == StatusType.Update ? "Synchronize" : kind == StatusType.Warning ? "Warning" : "Cancel.Round");
    }

    #endregion

    private void SuppressButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}