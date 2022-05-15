using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Util;

namespace ScreenToGif.Controls;

[DefaultProperty("Items")]
[ContentProperty("Items")]
public class NotificationBox : ItemsControl
{
    #region Variables

    /// <summary>
    /// The start point of the dragging operation.
    /// </summary>
    private Point _dragStart = new Point(0, 0);

    private Hyperlink _notificationHyperlink;
    private Hyperlink _encodingHyperlink;
    private ScrollViewer _mainScrollViewer;

    #endregion

    #region Properties

    public static readonly DependencyProperty HasAnyNotificationProperty = DependencyProperty.Register(nameof(HasAnyNotification), typeof(bool), typeof(NotificationBox), new PropertyMetadata(false));

    public static readonly DependencyProperty HasAnyEncodingProperty = DependencyProperty.Register(nameof(HasAnyEncoding), typeof(bool), typeof(NotificationBox), new PropertyMetadata(false));

    public static readonly DependencyProperty HasAnyActiveEncodingProperty = DependencyProperty.Register(nameof(HasAnyActiveEncoding), typeof(bool), typeof(NotificationBox), new PropertyMetadata(false));

    public static readonly DependencyProperty OnlyDisplayListProperty = DependencyProperty.Register(nameof(OnlyDisplayList), typeof(bool), typeof(NotificationBox), new PropertyMetadata(false));


    public bool HasAnyNotification
    {
        get => (bool) GetValue(HasAnyNotificationProperty);
        set => SetValue(HasAnyNotificationProperty, value);
    }

    public bool HasAnyEncoding
    {
        get => (bool)GetValue(HasAnyEncodingProperty);
        set => SetValue(HasAnyEncodingProperty, value);
    }

    public bool HasAnyActiveEncoding
    {
        get => (bool)GetValue(HasAnyActiveEncodingProperty);
        set => SetValue(HasAnyActiveEncodingProperty, value);
    }

    public bool OnlyDisplayList
    {
        get => (bool)GetValue(OnlyDisplayListProperty);
        set => SetValue(OnlyDisplayListProperty, value);
    }

    #endregion


    static NotificationBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NotificationBox), new FrameworkPropertyMetadata(typeof(NotificationBox)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _notificationHyperlink = GetTemplateChild("NotificationHyperlink") as Hyperlink;
        _encodingHyperlink = GetTemplateChild("EncodingHyperlink") as Hyperlink;
        _mainScrollViewer = GetTemplateChild("MainScrollViewer") as ScrollViewer;

        IsVisibleChanged += (sender, args) =>
        {
            if (!IsLoaded || !IsVisible)
                return;

            CheckIfFileExist();
        };

        if (_notificationHyperlink != null)
            _notificationHyperlink.Click += NotificationHyperlink_Click;

        if (_encodingHyperlink != null)
            _encodingHyperlink.Click += EncodingHyperlink_Click;

        UpdateNotification();
        UpdateEncoding();
    }

    #region Events

    private void Encoding_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        //Don't start the drag and drop if the user clicks on some button on the encoder.
        if (!(sender is EncoderListViewItem item) || e.OriginalSource is Run || VisualHelper.HasParent<ExtendedButton>(e.OriginalSource as Visual, typeof(EncoderListViewItem), true))
            return;

        item.CaptureMouse();
        _dragStart = e.GetPosition(null);
    }

    private void Encoding_MouseMove(object sender, MouseEventArgs e)
    {
        var diff = _dragStart - e.GetPosition(null);

        if (e.LeftButton != MouseButtonState.Pressed || !(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) ||
            !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;

        if (!(sender is EncoderListViewItem enc) || enc.Status != EncodingStatus.Completed || !File.Exists(enc.OutputFilename) || !enc.IsMouseCaptured)
            return;

        //To support multiple files in drag, use ListBox or ListView and get the selected items:
        //var files = ListView.SelectedItems.OfType<EncoderListViewItem>().Where(y => y.Status == Status.Completed && File.Exists(y.OutputFilename)).Select(x => x.OutputFilename).ToArray();

        DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, new[] { enc.OutputFilename }), Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? DragDropEffects.Copy : DragDropEffects.Move);
    }

    private void Encoding_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var item = sender as UIElement;
        item?.ReleaseMouseCapture();
    }

    private void CancelEncoding_Clicked(object sender, RoutedEventArgs args)
    {
        if (!(sender is EncoderListViewItem item))
            return;

        if (item.Status != EncodingStatus.Processing)
            EncodingManager.RemoveEncodings(item.Id);
        else if (!item.TokenSource.IsCancellationRequested)
            item.TokenSource.Cancel();
    }

    private void RemoveNotification_Click(object sender, RoutedEventArgs args)
    {
        if (!(sender is StatusBand band))
            return;

        NotificationManager.RemoveNotification(band.Id);
    }

    private void NotificationHyperlink_Click(object sender, RoutedEventArgs args)
    {
        NotificationManager.RemoveAllNotifications();
    }

    private void EncodingHyperlink_Click(object sender, RoutedEventArgs args)
    {
        EncodingManager.RemoveFinishedEncodings();
    }

    #endregion

    #region UI manipulation

    public void UpdateNotification(int? id = null)
    {
        var evergreen = id.HasValue ? NotificationManager.Notifications.Where(w => w.Id == id.Value).ToList() : NotificationManager.Notifications;
        var dirty = id.HasValue ? Items.OfType<StatusBand>().Where(w => w.Id == id).ToList() : Items.OfType<StatusBand>().ToList();

        //Add items.
        var include = evergreen.Where(w => dirty.All(a => a.Id != w.Id)).ToList();

        foreach (var item in include)
        {
            var not = new StatusBand
            {
                Id = item.Id,
                Text = item.Text,
                Type = item.Kind,
                Action = item.Action,
                IsLink = item.Action != null,
                Visibility = Visibility.Visible
            };
            not.Dismissed += RemoveNotification_Click;

            Items.Add(not);
        }

        //Remove items that dont exist anymore.
        var remove = dirty.Where(w => evergreen.All(a => a.Id != w.Id)).ToList();

        foreach (var item in remove)
        {
            item.Dismissed -= RemoveNotification_Click;

            Items.Remove(item);
        }

        //Update others.
        var update = evergreen.Where(w => dirty.Any(a => a.Id == w.Id)).ToList();

        foreach (var item in update)
        {
            var actual = Items.OfType<StatusBand>().FirstOrDefault(w => w.Id == item.Id);

            if (actual == null)
                continue;

            actual.Text = item.Text;
            //TODO: Should this exist?
        }

        CommandManager.InvalidateRequerySuggested();
        GC.Collect();

        HasAnyNotification = Items.OfType<StatusBand>().Any();
    }

    public EncoderListViewItem AddEncoding(int id)
    {
        var item = EncodingManager.Encodings.FirstOrDefault(w => w.Id == id);

        if (item == null)
            return null;

        //Check if the enoder item was added before, during initialization.
        if (Items.OfType<EncoderListViewItem>().Any(a => a.Id == id))
            return null;

        var enc = new EncoderListViewItem
        {
            Id = item.Id,
            OutputType = item.OutputType,
            Text = item.Text,
            Status = item.Status,
            CurrentFrame = item.CurrentFrame,
            FrameCount = item.FrameCount,
            TokenSource = item.TokenSource,

            //These following properties are only available if an IEncoding window is opened after an encoding was already inserted.
            IsIndeterminate = item.IsIndeterminate,
            SizeInBytes = item.SizeInBytes,
            OutputFilename = item.OutputFilename,
            SavedToDisk = item.SavedToDisk,

            Uploaded = item.Uploaded,
            UploadLink = item.UploadLink,
            UploadLinkDisplay = item.UploadLinkDisplay,
            DeletionLink = item.DeletionLink,
            Exception = item.Exception,

            CommandExecuted = item.CommandExecuted,
            Command = item.Command,
            CommandOutput = item.CommandOutput,
            CommandTaskException = item.CommandTaskException,

            CopiedToClipboard = item.CopiedToClipboard,
            CopyTaskException = item.CopyTaskException,

            TimeToAnalyze = item.TimeToAnalyze,
            TimeToEncode = item.TimeToEncode,
            TimeToUpload = item.TimeToUpload,
            TimeToCopy = item.TimeToCopy,
            TimeToExecute = item.TimeToExecute,
        };
        enc.CancelClicked += CancelEncoding_Clicked;
        enc.PreviewMouseLeftButtonDown += Encoding_PreviewMouseLeftButtonDown;
        enc.PreviewMouseLeftButtonUp += Encoding_PreviewMouseLeftButtonUp;
        enc.MouseMove += Encoding_MouseMove;

        Items.Add(enc);

        _mainScrollViewer?.ScrollToBottom();

        HasAnyEncoding = Items.OfType<EncoderListViewItem>().Any();
        HasAnyActiveEncoding = Items.OfType<EncoderListViewItem>().Any(a => a.Status == EncodingStatus.Processing);

        return enc;
    }

    public void UpdateEncoding(int? id = null)
    {
        var evergreen = id.HasValue ? EncodingManager.Encodings.Where(w => w.Id == id.Value).ToList() : EncodingManager.Encodings;
        var dirty = id.HasValue ? Items.OfType<EncoderListViewItem>().Where(w => w.Id == id).ToList() : Items.OfType<EncoderListViewItem>().ToList();

        //Add items.
        var include = evergreen.Where(w => dirty.All(a => a.Id != w.Id)).ToList();

        foreach (var item in include)
        {
            var enc = new EncoderListViewItem
            {
                Id = item.Id,
                OutputType = item.OutputType,
                Text = item.Text,
                Status = item.Status,
                CurrentFrame = item.CurrentFrame,
                FrameCount = item.FrameCount,
                TokenSource = item.TokenSource,

                //These following properties are only available if an IEncoding window is opened after an encoding was already inserted.
                IsIndeterminate = item.IsIndeterminate,
                SizeInBytes = item.SizeInBytes,
                OutputFilename = item.OutputFilename,
                SavedToDisk = item.SavedToDisk,

                Uploaded = item.Uploaded,
                UploadLink = item.UploadLink,
                UploadLinkDisplay = item.UploadLinkDisplay,
                DeletionLink = item.DeletionLink,
                Exception = item.Exception,

                CommandExecuted = item.CommandExecuted,
                Command = item.Command,
                CommandOutput = item.CommandOutput,
                CommandTaskException = item.CommandTaskException,

                CopiedToClipboard = item.CopiedToClipboard,
                CopyTaskException = item.CopyTaskException,

                TimeToAnalyze = item.TimeToAnalyze,
                TimeToEncode = item.TimeToEncode,
                TimeToUpload = item.TimeToUpload,
                TimeToCopy = item.TimeToCopy,
                TimeToExecute = item.TimeToExecute,
            };
            enc.CancelClicked += CancelEncoding_Clicked;
            enc.PreviewMouseLeftButtonDown += Encoding_PreviewMouseLeftButtonDown;
            enc.PreviewMouseLeftButtonUp += Encoding_PreviewMouseLeftButtonUp;
            enc.MouseMove += Encoding_MouseMove;

            EncodingManager.ViewList.Add(enc);
            Items.Add(enc);
        }

        //Remove items that dont exist anymore.
        var remove = dirty.Where(w => evergreen.All(a => a.Id != w.Id)).ToList();

        foreach (var item in remove)
        {
            item.CancelClicked -= CancelEncoding_Clicked;
            item.PreviewMouseLeftButtonDown -= Encoding_PreviewMouseLeftButtonDown;
            item.PreviewMouseLeftButtonUp -= Encoding_PreviewMouseLeftButtonUp;
            item.MouseMove -= Encoding_MouseMove;

            EncodingManager.ViewList.Remove(item);
            Items.Remove(item);
        }

        //Update others.
        var update = evergreen.Where(w => dirty.Any(a => a.Id == w.Id)).ToList();

        foreach (var item in update)
        {
            //var current = EncodingManager.Encodings.FirstOrDefault(w => w.Id == item.Id);
            var actual = Items.OfType<EncoderListViewItem>().FirstOrDefault(w => w.Id == item.Id);

            if (actual == null)
                continue;

            actual.Text = item.Text;
            actual.Status = item.Status;
            actual.IsIndeterminate = item.IsIndeterminate;
            actual.CurrentFrame = item.CurrentFrame;
            actual.FrameCount = item.FrameCount;
            actual.SizeInBytes = item.SizeInBytes;
            actual.OutputFilename = item.OutputFilename;
            actual.SavedToDisk = item.SavedToDisk;
            actual.Exception = item.Exception;

            actual.Uploaded = item.Uploaded;
            actual.UploadLink = item.UploadLink;
            actual.UploadLinkDisplay = item.UploadLinkDisplay;
            actual.DeletionLink = item.DeletionLink;

            actual.CommandExecuted = item.CommandExecuted;
            actual.Command = item.Command;
            actual.CommandOutput = item.CommandOutput;
            actual.CommandTaskException = item.CommandTaskException;

            actual.CopiedToClipboard = item.CopiedToClipboard;
            actual.CopyTaskException = item.CopyTaskException;

            actual.TimeToAnalyze = item.TimeToAnalyze;
            actual.TimeToEncode = item.TimeToEncode;
            actual.TimeToUpload = item.TimeToUpload;
            actual.TimeToCopy = item.TimeToCopy;
            actual.TimeToExecute = item.TimeToExecute;
        }

        CommandManager.InvalidateRequerySuggested();
        GC.Collect();

        HasAnyEncoding = Items.OfType<EncoderListViewItem>().Any();
        HasAnyActiveEncoding = Items.OfType<EncoderListViewItem>().Any(a => a.Status == EncodingStatus.Processing);
    }

    public EncoderListViewItem RemoveEncoding(int id)
    {
        //Removes encoding.
        var item = EncodingManager.Encodings.FirstOrDefault(w => w.Id == id);

        if (item != null)
            Items.Remove(item);

        //Removes view.
        var enc = Items.OfType<EncoderListViewItem>().FirstOrDefault(w => w.Id == id);

        if (enc == null)
            return null;

        enc.CancelClicked -= CancelEncoding_Clicked;
        enc.PreviewMouseLeftButtonDown -= Encoding_PreviewMouseLeftButtonDown;
        enc.PreviewMouseLeftButtonUp -= Encoding_PreviewMouseLeftButtonUp;
        enc.MouseMove -= Encoding_MouseMove;

        EncodingManager.ViewList.Remove(enc);
        Items.Remove(enc);

        HasAnyEncoding = Items.OfType<EncoderListViewItem>().Any();
        HasAnyActiveEncoding = Items.OfType<EncoderListViewItem>().Any(a => a.Status == EncodingStatus.Processing);

        return enc;
    }

    public void CheckIfFileExist()
    {
        foreach (var item in EncodingManager.Encodings.Where(item => item.Status == EncodingStatus.Completed || item.Status == EncodingStatus.FileDeletedOrMoved))
        {
            if (!File.Exists(item.OutputFilename) && !item.AreMultipleFiles)
                EncodingManager.Update(item.Id, EncodingStatus.FileDeletedOrMoved);
            else if (item.Status == EncodingStatus.FileDeletedOrMoved)
                EncodingManager.Update(item.Id, EncodingStatus.Completed, item.OutputFilename);
        }
    }

    /// <summary>
    /// Removes all views from this instance.
    /// This method is used when the encoder window closes and needs to remove the references from the manager.
    /// </summary>
    public void RemoveAllViews()
    {
        var list = Items.OfType<EncoderListViewItem>().ToList();

        foreach (var enc in list)
        {
            enc.CancelClicked -= CancelEncoding_Clicked;
            enc.PreviewMouseLeftButtonDown -= Encoding_PreviewMouseLeftButtonDown;
            enc.MouseMove -= Encoding_MouseMove;

            EncodingManager.ViewList.Remove(enc);
            Items.Remove(enc);
        }
    }

    #endregion
}