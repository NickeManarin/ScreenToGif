using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.ImageUtil;
using ScreenToGif.Util;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Windows.Other;
using Clipboard = System.Windows.Clipboard;

namespace ScreenToGif.Controls;

/// <summary>
/// ListViewItem used by the Encoder window.
/// </summary>
public class EncoderListViewItem : ListViewItem
{
    #region Dependency Properties

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(EncoderListViewItem));

    public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register(nameof(Percentage), typeof(double), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(0.0));

    public static readonly DependencyProperty CurrentFrameProperty = DependencyProperty.Register(nameof(CurrentFrame), typeof(int), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(1));

    public static readonly DependencyProperty FrameCountProperty = DependencyProperty.Register(nameof(FrameCount), typeof(int), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(0));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata());

    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(int), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(-1));

    public static readonly DependencyProperty TokenSourceProperty = DependencyProperty.Register(nameof(TokenSource), typeof(CancellationTokenSource), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata());

    public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(EncodingStatus), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(EncodingStatus.Processing));

    public static readonly DependencyProperty OutputTypeProperty = DependencyProperty.Register(nameof(OutputType), typeof(ExportFormats), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(ExportFormats.Gif));

    public static readonly DependencyProperty SizeInBytesProperty = DependencyProperty.Register(nameof(SizeInBytes), typeof(long), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(0L));

    public static readonly DependencyProperty OutputPathProperty = DependencyProperty.Register(nameof(OutputPath), typeof(string), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata());

    public static readonly DependencyProperty OutputFilenameProperty = DependencyProperty.Register(nameof(OutputFilename), typeof(string), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(OutputFilename_PropertyChanged));

    public static readonly DependencyProperty SavedToDiskProperty = DependencyProperty.Register(nameof(SavedToDisk), typeof(bool), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty AreMultipleFilesProperty = DependencyProperty.Register(nameof(AreMultipleFiles), typeof(bool), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty ExceptionProperty = DependencyProperty.Register(nameof(Exception), typeof(Exception), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata());


    public static readonly DependencyProperty UploadedProperty = DependencyProperty.Register(nameof(Uploaded), typeof(bool), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty UploadLinkProperty = DependencyProperty.Register(nameof(UploadLink), typeof(string), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata());

    public static readonly DependencyProperty UploadLinkDisplayProperty = DependencyProperty.Register(nameof(UploadLinkDisplay), typeof(string), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata());

    public static readonly DependencyProperty DeletionLinkProperty = DependencyProperty.Register(nameof(DeletionLink), typeof(string), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata());

    public static readonly DependencyProperty UploadTaskExceptionProperty = DependencyProperty.Register(nameof(UploadTaskException), typeof(Exception), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(null));


    public static readonly DependencyProperty CopiedToClipboardProperty = DependencyProperty.Register(nameof(CopiedToClipboard), typeof(bool), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty CopyTaskExceptionProperty = DependencyProperty.Register(nameof(CopyTaskException), typeof(Exception), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(null));


    public static readonly DependencyProperty CommandExecutedProperty = DependencyProperty.Register(nameof(CommandExecuted), typeof(bool), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty CommandTaskExceptionProperty = DependencyProperty.Register(nameof(CommandTaskException), typeof(Exception), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(string), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty CommandOutputProperty = DependencyProperty.Register(nameof(CommandOutput), typeof(string), typeof(EncoderListViewItem),
        new FrameworkPropertyMetadata(null));


    public static readonly DependencyProperty TotalTimeProperty = DependencyProperty.Register(nameof(TotalTime), typeof(TimeSpan), typeof(EncoderListViewItem),
        new PropertyMetadata(TimeSpan.Zero));

    public static readonly DependencyProperty TimeToAnalyzeProperty = DependencyProperty.Register(nameof(TimeToAnalyze), typeof(TimeSpan), typeof(EncoderListViewItem),
        new PropertyMetadata(TimeSpan.Zero, TimeSpan_PropertyChanged));

    public static readonly DependencyProperty TimeToEncodeProperty = DependencyProperty.Register(nameof(TimeToEncode), typeof(TimeSpan), typeof(EncoderListViewItem),
        new PropertyMetadata(TimeSpan.Zero, TimeSpan_PropertyChanged));

    public static readonly DependencyProperty TimeToUploadProperty = DependencyProperty.Register(nameof(TimeToUpload), typeof(TimeSpan), typeof(EncoderListViewItem),
        new PropertyMetadata(TimeSpan.Zero, TimeSpan_PropertyChanged));

    public static readonly DependencyProperty TimeToCopyProperty = DependencyProperty.Register(nameof(TimeToCopy), typeof(TimeSpan), typeof(EncoderListViewItem),
        new PropertyMetadata(TimeSpan.Zero, TimeSpan_PropertyChanged));

    public static readonly DependencyProperty TimeToExecuteProperty = DependencyProperty.Register(nameof(TimeToExecute), typeof(TimeSpan), typeof(EncoderListViewItem),
        new PropertyMetadata(TimeSpan.Zero, TimeSpan_PropertyChanged));

    #endregion

    #region Properties

    /// <summary>
    /// The icon of the ListViewItem.
    /// </summary>
    [Description("The icon of the ListViewItem.")]
    public Brush Icon
    {
        get => (Brush)GetValue(IconProperty);
        set => SetCurrentValue(IconProperty, value);
    }

    /// <summary>
    /// The encoding percentage.
    /// </summary>
    [Description("The encoding percentage.")]
    public double Percentage
    {
        get => (double)GetValue(PercentageProperty);
        set => SetCurrentValue(PercentageProperty, value);
    }

    /// <summary>
    /// The current frame being processed.
    /// </summary>
    [Description("The frame count.")]
    public int CurrentFrame
    {
        get => (int)GetValue(CurrentFrameProperty);
        set
        {
            SetCurrentValue(CurrentFrameProperty, value);

            if (CurrentFrame == 0)
            {
                Percentage = 0;
                return;
            }

            // 100% = FrameCount
            // 100% * CurrentFrame / FrameCount = Actual Percentage
            Percentage = Math.Round(CurrentFrame * 100.0 / FrameCount, 1, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// The frame count.
    /// </summary>
    [Description("The frame count.")]
    public int FrameCount
    {
        get => (int)GetValue(FrameCountProperty);
        set => SetCurrentValue(FrameCountProperty, value);
    }

    /// <summary>
    /// The description of the item.
    /// </summary>
    [Description("The description of the item.")]
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetCurrentValue(TextProperty, value);
    }

    /// <summary>
    /// The ID of the Task.
    /// </summary>
    [Description("The ID of the Task.")]
    public int Id
    {
        get => (int)GetValue(IdProperty);
        set => SetCurrentValue(IdProperty, value);
    }

    /// <summary>
    /// The Cancellation Token Source.
    /// </summary>
    [Description("The Cancellation Token Source.")]
    public CancellationTokenSource TokenSource
    {
        get => (CancellationTokenSource)GetValue(TokenSourceProperty);
        set => SetCurrentValue(TokenSourceProperty, value);
    }

    /// <summary>
    /// The state of the progress bar.
    /// </summary>
    [Description("The state of the progress bar.")]
    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetCurrentValue(IsIndeterminateProperty, value);
    }

    /// <summary>
    /// The status of the encoding.
    /// </summary>
    [Description("The status of the encoding.")]
    public EncodingStatus Status
    {
        get => (EncodingStatus)GetValue(StatusProperty);
        set => SetCurrentValue(StatusProperty, value);
    }

    /// <summary>
    /// The size of the output file in bytes.
    /// </summary>
    [Description("The size of the output file in bytes.")]
    public long SizeInBytes
    {
        get => (long)GetValue(SizeInBytesProperty);
        set => SetCurrentValue(SizeInBytesProperty, value);
    }

    /// <summary>
    /// The filename of the output file.
    /// </summary>
    [Description("The filename of the output file.")]
    public string OutputFilename
    {
        get => (string)GetValue(OutputFilenameProperty);
        set => SetCurrentValue(OutputFilenameProperty, value);
    }

    /// <summary>
    /// The path of the output file.
    /// </summary>
    [Description("The path of the output file.")]
    public string OutputPath
    {
        get => (string)GetValue(OutputPathProperty);
        set => SetCurrentValue(OutputPathProperty, value);
    }

    /// <summary>
    /// True if the outfile file was saved to disk.
    /// </summary>
    [Description("True if the outfile file was saved to disk.")]
    public bool SavedToDisk
    {
        get => (bool)GetValue(SavedToDiskProperty);
        set => SetCurrentValue(SavedToDiskProperty, value);
    }

    /// <summary>
    /// True if the exporter exported multiple files.
    /// </summary>
    [Description("True if the exporter exported multiple files.")]
    public bool AreMultipleFiles
    {
        get => (bool)GetValue(AreMultipleFilesProperty);
        set => SetCurrentValue(AreMultipleFilesProperty, value);
    }

    /// <summary>
    /// The type of the output.
    /// </summary>
    [Description("The type of the output.")]
    public ExportFormats OutputType
    {
        get => (ExportFormats)GetValue(OutputTypeProperty);
        set => SetCurrentValue(OutputTypeProperty, value);
    }

    /// <summary>
    /// The exception of the encoding.
    /// </summary>
    [Description("The exception of the encoding.")]
    public Exception Exception
    {
        get => (Exception)GetValue(ExceptionProperty);
        set => SetCurrentValue(ExceptionProperty, value);
    }

    /// <summary>
    /// True if the outfile file was uploaded.
    /// </summary>
    [Description("True if the outfile file was uploaded.")]
    public bool Uploaded
    {
        get => (bool)GetValue(UploadedProperty);
        set => SetCurrentValue(UploadedProperty, value);
    }

    /// <summary>
    /// The link to the uploaded file.
    /// </summary>
    [Description("The link to the uploaded file.")]
    public string UploadLink
    {
        get => (string)GetValue(UploadLinkProperty);
        set => SetCurrentValue(UploadLinkProperty, value);
    }

    /// <summary>
    /// The link to the uploaded file (without the http).
    /// </summary>
    [Description("The link to the uploaded file (without the http).")]
    public string UploadLinkDisplay
    {
        get => (string)GetValue(UploadLinkDisplayProperty);
        set => SetCurrentValue(UploadLinkDisplayProperty, value);
    }

    /// <summary>
    /// The link to delete the uploaded file.
    /// </summary>
    [Description("The link to delete the uploaded file.")]
    public string DeletionLink
    {
        get => (string)GetValue(DeletionLinkProperty);
        set => SetCurrentValue(DeletionLinkProperty, value);
    }

    /// <summary>
    /// The exception detail about the upload task.
    /// </summary>
    [Description("The exception detail about the upload task.")]
    public Exception UploadTaskException
    {
        get => (Exception)GetValue(UploadTaskExceptionProperty);
        set => SetCurrentValue(UploadTaskExceptionProperty, value);
    }



    /// <summary>
    /// True if the outfile file was copied to the clipboard.
    /// </summary>
    [Description("True if the outfile file was copied to the clipboard.")]
    public bool CopiedToClipboard
    {
        get => (bool)GetValue(CopiedToClipboardProperty);
        set => SetCurrentValue(CopiedToClipboardProperty, value);
    }

    /// <summary>
    /// The exception detail about the copy task.
    /// </summary>
    [Description("The exception detail about the copy task.")]
    public Exception CopyTaskException
    {
        get => (Exception)GetValue(CopyTaskExceptionProperty);
        set => SetCurrentValue(CopyTaskExceptionProperty, value);
    }



    /// <summary>
    /// True if the post encoding commands were executed.
    /// </summary>
    [Description("True if the post encoding commands were executed.")]
    public bool CommandExecuted
    {
        get => (bool)GetValue(CommandExecutedProperty);
        set => SetCurrentValue(CommandExecutedProperty, value);
    }

    /// <summary>
    /// The exception detail about the post encoding command task.
    /// </summary>
    [Description("The exception detail about the post encoding command task.")]
    public Exception CommandTaskException
    {
        get => (Exception)GetValue(CommandTaskExceptionProperty);
        set => SetCurrentValue(CommandTaskExceptionProperty, value);
    }

    /// <summary>
    /// The command that was executed.
    /// </summary>
    [Description("The command that was executed.")]
    public string Command
    {
        get => (string)GetValue(CommandProperty);
        set => SetCurrentValue(CommandProperty, value);
    }

    /// <summary>
    /// The output from the post encoding commands.
    /// </summary>
    [Description("The output from the post encoding commands.")]
    public string CommandOutput
    {
        get => (string)GetValue(CommandOutputProperty);
        set => SetCurrentValue(CommandOutputProperty, value);
    }


    /// <summary>
    /// The total time to finish the process.
    /// </summary>
    [Description("The total time to finish the process.")]
    public TimeSpan TotalTime
    {
        get => (TimeSpan)GetValue(TotalTimeProperty);
        set => SetValue(TotalTimeProperty, value);
    }

    /// <summary>
    /// The time it took to analyze the frames.
    /// </summary>
    [Description("The time it took to analyze the frames.")]
    public TimeSpan TimeToAnalyze
    {
        get => (TimeSpan)GetValue(TimeToAnalyzeProperty);
        set => SetValue(TimeToAnalyzeProperty, value);
    }

    /// <summary>
    /// The time it took to encode the frames.
    /// </summary>
    [Description("The time it took to encode the frames.")]
    public TimeSpan TimeToEncode
    {
        get => (TimeSpan)GetValue(TimeToEncodeProperty);
        set => SetValue(TimeToEncodeProperty, value);
    }

    /// <summary>
    /// The time it took to upload the file.
    /// </summary>
    [Description("The time it took to upload the file.")]
    public TimeSpan TimeToUpload
    {
        get => (TimeSpan)GetValue(TimeToUploadProperty);
        set => SetValue(TimeToUploadProperty, value);
    }

    /// <summary>
    /// The time it took to copy the file.
    /// </summary>
    [Description("The time it took to copy the file.")]
    public TimeSpan TimeToCopy
    {
        get => (TimeSpan)GetValue(TimeToCopyProperty);
        set => SetValue(TimeToCopyProperty, value);
    }

    /// <summary>
    /// The time it took to execute the post encoding commands.
    /// </summary>
    [Description("The time it took to execute the post encoding commands.")]
    public TimeSpan TimeToExecute
    {
        get => (TimeSpan)GetValue(TimeToExecuteProperty);
        set => SetValue(TimeToExecuteProperty, value);
    }

    #endregion

    #region Custom Events

    public static readonly RoutedEvent CancelClickedEvent = EventManager.RegisterRoutedEvent("CancelClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EncoderListViewItem));

    public static readonly RoutedEvent OpenFileClickedEvent = EventManager.RegisterRoutedEvent("OpenFileClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EncoderListViewItem));

    public static readonly RoutedEvent ExploreFolderClickedEvent = EventManager.RegisterRoutedEvent("ExploreFolderClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EncoderListViewItem));

    /// <summary>
    /// Event raised when the user clicks on the cancel button.
    /// </summary>
    public event RoutedEventHandler CancelClicked
    {
        add => AddHandler(CancelClickedEvent, value);
        remove => RemoveHandler(CancelClickedEvent, value);
    }

    /// <summary>
    /// Event raised when the user clicks on the "Open file" button.
    /// </summary>
    public event RoutedEventHandler OpenFileClicked
    {
        add => AddHandler(OpenFileClickedEvent, value);
        remove => RemoveHandler(OpenFileClickedEvent, value);
    }

    /// <summary>
    /// Event raised when the user clicks on the "Explore folder" button.
    /// </summary>
    public event RoutedEventHandler ExploreFolderClicked
    {
        add => AddHandler(ExploreFolderClickedEvent, value);
        remove => RemoveHandler(ExploreFolderClickedEvent, value);
    }

    public void RaiseCancelClickedEvent()
    {
        if (CancelClickedEvent == null || !IsLoaded)
            return;

        var newEventArgs = new RoutedEventArgs(CancelClickedEvent);
        RaiseEvent(newEventArgs);
    }

    public void RaiseOpenFileClickedEvent()
    {
        if (OpenFileClickedEvent == null || !IsLoaded)
            return;

        var newEventArgs = new RoutedEventArgs(OpenFileClickedEvent);
        RaiseEvent(newEventArgs);
    }

    public void RaiseExploreFolderClickedEvent()
    {
        if (ExploreFolderClickedEvent == null || !IsLoaded)
            return;

        var newEventArgs = new RoutedEventArgs(ExploreFolderClickedEvent);
        RaiseEvent(newEventArgs);
    }

    #endregion

    static EncoderListViewItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(EncoderListViewItem), new FrameworkPropertyMetadata(typeof(EncoderListViewItem)));
    }


    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var cancelButton = Template.FindName("CancelButton", this) as ExtendedButton;

        var copyFailedHyperlink = Template.FindName("CopyFailedHyperlink", this) as Hyperlink;
        var executedHyperlink = Template.FindName("ExecutedHyperlink", this) as Hyperlink;
        var executionFailedHyperlink = Template.FindName("ExecutionFailedHyperlink", this) as Hyperlink;
        var uploadHyperlink = Template.FindName("UploadHyperlink", this) as Hyperlink;
        var uploadFailedHyperlink = Template.FindName("UploadFailedHyperlink", this) as Hyperlink;

        var fileButton = Template.FindName("FileButton", this) as ExtendedButton;
        var folderButton = Template.FindName("FolderButton", this) as ExtendedButton;
        var detailsButton = Template.FindName("DetailsButton", this) as ExtendedButton;
        var copyMenu = Template.FindName("CopyMenuItem", this) as ExtendedMenuItem;
        var copyImageMenu = Template.FindName("CopyImageMenuItem", this) as ExtendedMenuItem;
        var copyFilenameMenu = Template.FindName("CopyFilenameMenuItem", this) as ExtendedMenuItem;
        var copyFolderMenu = Template.FindName("CopyFolderMenuItem", this) as ExtendedMenuItem;
        var copyLinkMenu = Template.FindName("CopyLinkMenuItem", this) as ExtendedMenuItem;

        if (cancelButton != null)
            cancelButton.Click += (s, a) => RaiseCancelClickedEvent();

        //Copy failed.
        if (copyFailedHyperlink != null)
            copyFailedHyperlink.Click += (s, a) =>
            {
                if (CopyTaskException == null) return;

                var viewer = new ExceptionViewer(CopyTaskException);
                viewer.ShowDialog();
            };

        //Command executed.
        if (executedHyperlink != null)
            executedHyperlink.Click += (s, a) =>
            {
                var dialog = new TextDialog { Command = Command, Output = CommandOutput };
                dialog.ShowDialog();
            };

        //Command execution failed.
        if (executionFailedHyperlink != null)
            executionFailedHyperlink.Click += (s, a) =>
            {
                if (CommandTaskException == null) return;

                var viewer = new ExceptionViewer(CommandTaskException);
                viewer.ShowDialog();
            };

        //Upload done.
        if (uploadHyperlink != null)
            uploadHyperlink.Click += (s, a) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(UploadLink))
                        return;

                    ProcessHelper.StartWithShell(Keyboard.Modifiers != ModifierKeys.Control || string.IsNullOrWhiteSpace(DeletionLink) ? UploadLink : DeletionLink);
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "Error while opening the upload link");
                }
            };

        //Upload failed.
        if (uploadFailedHyperlink != null)
            uploadFailedHyperlink.Click += (s, a) =>
            {
                if (UploadTaskException == null) return;

                var viewer = new ExceptionViewer(UploadTaskException);
                viewer.ShowDialog();
            };

        //Open file.
        if (fileButton != null)
            fileButton.Click += (s, a) =>
            {
                RaiseOpenFileClickedEvent();

                try
                {
                    if (!string.IsNullOrWhiteSpace(OutputFilename) && File.Exists(OutputFilename))
                        ProcessHelper.StartWithShell(OutputFilename);
                }
                catch (Exception ex)
                {
                    Dialog.Ok("Open File", "Error while opening the file", ex.Message);
                }
            };

        //Open folder.
        if (folderButton != null)
            folderButton.Click += (s, a) =>
            {
                RaiseExploreFolderClickedEvent();

                try
                {
                    if (!string.IsNullOrWhiteSpace(OutputFilename) && Directory.Exists(OutputPath))
                        Process.Start("explorer.exe", $"/select,\"{OutputFilename.Replace("/", "\\")}\"");
                }
                catch (Exception ex)
                {
                    Dialog.Ok("Explore Folder", "Error while opening the folder", ex.Message);
                }
            };

        //Details. Usually when something wrong happens.
        if (detailsButton != null)
            detailsButton.Click += (s, a) =>
            {
                if (Exception == null) return;

                var viewer = new ExceptionViewer(Exception);
                viewer.ShowDialog();
            };

        //Copy (as image and text).
        if (copyMenu != null)
            copyMenu.Click += (s, a) =>
            {
                if (string.IsNullOrWhiteSpace(OutputFilename))
                    return;

                var data = new DataObject();
                data.SetFileDropList(new StringCollection { OutputFilename });

                SetClipboard(data);
            };

        //Copy as image.
        if (copyImageMenu != null)
            copyImageMenu.Click += (s, a) =>
            {
                if (string.IsNullOrWhiteSpace(OutputFilename))
                    return;

                var data = new DataObject();
                data.SetImage(OutputFilename.SourceFrom());

                SetClipboard(data);
            };

        //Copy full path.
        if (copyFilenameMenu != null)
            copyFilenameMenu.Click += (s, a) =>
            {
                if (string.IsNullOrWhiteSpace(OutputFilename))
                    return;

                var data = new DataObject();
                data.SetText(OutputFilename, TextDataFormat.Text);

                SetClipboard(data);
            };

        //Copy folder path.
        if (copyFolderMenu != null)
            copyFolderMenu.Click += (s, a) =>
            {
                if (string.IsNullOrWhiteSpace(OutputPath))
                    return;

                var data = new DataObject();
                data.SetText(OutputPath, TextDataFormat.Text);

                SetClipboard(data);
            };

        // Copy link
        if (copyLinkMenu != null)
        {
            copyLinkMenu.Click += (s, a) =>
            {
                if (string.IsNullOrWhiteSpace(UploadLink))
                    return;

                var data = new DataObject();
                data.SetText(UploadLink, TextDataFormat.Text);

                SetClipboard(data);
            };
        }
    }


    private static void OutputFilename_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is EncoderListViewItem item))
            return;

        item.OutputPath = Path.GetDirectoryName(item.OutputFilename);
    }

    private static void TimeSpan_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is EncoderListViewItem item))
            return;

        item.TotalTime = item.TimeToAnalyze + item.TimeToEncode + item.TimeToUpload + item.TimeToCopy + item.TimeToExecute;
    }


    private void SetClipboard(DataObject data)
    {
        //It tries to set the data to the clipboard 10 times before failing it to do so.
        //This issue may happen if the clipboard is opened by any clipboard manager.
        for (var i = 0; i < 10; i++)
        {
            try
            {
                Clipboard.SetDataObject(data, true);
                break;
            }
            catch (COMException ex)
            {
                if ((uint)ex.ErrorCode != 0x800401D0) //CLIPBRD_E_CANT_OPEN
                    throw;
            }

            Thread.Sleep(100);
        }
    }
}