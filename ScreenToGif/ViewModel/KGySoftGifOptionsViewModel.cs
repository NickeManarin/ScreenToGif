#region Usings

#region Used Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;

using KGySoft.ComponentModel;
using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;
using KGySoft.Drawing.Wpf;
using KGySoft.Threading;

using ScreenToGif.Util;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;

#endregion

#region Used Aliases

using Color = System.Windows.Media.Color;

#endregion

#endregion

namespace ScreenToGif.ViewModel;

/// <summary>
/// Provides the ViewModel class for the <see cref="KGySoftGifPreset"/> model type.
/// </summary>
public class KGySoftGifOptionsViewModel : ObservableObjectBase
{
    #region Fields

    #region Static Fields

    private static readonly HashSet<string> _affectsPreview = new()
    {
        // quantizer settings
        nameof(QuantizerId),
        nameof(BackColor),
        nameof(AlphaThreshold),
        nameof(WhiteThreshold),
        nameof(DirectMapping),
        nameof(PaletteSize),
        nameof(BitLevel),

        // ditherer settings
        nameof(DithererId),
        nameof(Strength),
        nameof(Seed),
        nameof(IsSerpentineProcessing),

        // preview settings
        nameof(ShowCurrentFrame)
    };

    private static readonly HashSet<string> _affectsNotifications = new()
    {
        nameof(QuantizerId),
        nameof(AlphaThreshold),
        nameof(AllowDeltaFrames),
        nameof(AllowClippedFrames),
        nameof(DeltaTolerance),
    };
    
    private static bool _lastShowCurrentFrame;
    private static string _lastDitherer;

    #endregion

    #region Instance Fields

    private readonly KGySoftGifPreset _preset;

    private WriteableBitmap _previewBitmap;
    private IReadableBitmapData _currentFrame;
    private CancellationTokenSource _cancelGeneratingPreview;
    private Task _generatePreviewTask;

    #endregion

    #endregion

    #region Properties

    // Quantizer
    public QuantizerDescriptor[] Quantizers => QuantizerDescriptor.Quantizers;
    public string QuantizerId { get => Get(_preset.QuantizerId ?? QuantizerDescriptor.Quantizers[0].Id); set => Set(_preset.QuantizerId = value); }
    public Color BackColor { get => Get(_preset.BackColor); set => Set(_preset.BackColor = value); }
    public byte AlphaThreshold { get => Get(_preset.AlphaThreshold); set => Set(_preset.AlphaThreshold = value); }
    public byte WhiteThreshold { get => Get(_preset.WhiteThreshold); set => Set(_preset.WhiteThreshold = value); }
    public bool DirectMapping { get => Get(_preset.DirectMapping); set => Set(_preset.DirectMapping = value); }
    public int PaletteSize { get => Get(_preset.PaletteSize); set => Set(_preset.PaletteSize = value); }
    public byte? BitLevel { get => Get(_preset.BitLevel); set => Set(_preset.BitLevel = value); }
    public bool IsCustomBitLevel { get => Get(_preset.BitLevel.HasValue); set => Set(value); }

    // Ditherer
    public bool UseDitherer { get => Get(_preset.DithererId != null); set => Set(value); }
    public DithererDescriptor[] Ditherers => DithererDescriptor.Ditherers;
    public string DithererId { get => Get(_preset.DithererId); set => Set(_preset.DithererId = value); }
    public float Strength { get => Get(_preset.Strength); set => Set(_preset.Strength = value); }
    public int? Seed { get => Get(_preset.Seed); set => Set(_preset.Seed = value); }
    public bool IsSerpentineProcessing { get => Get(_preset.IsSerpentineProcessing); set => Set(_preset.IsSerpentineProcessing = value); }

    // Preview
    public bool ShowCurrentFrame { get => Get(_lastShowCurrentFrame); set => Set(_lastShowCurrentFrame = value); }
    public string CurrentFramePath { get => Get<string>(); set => Set(value); }
    public bool IsGenerating { get => Get<bool>(); set => Set(value); }
    public WriteableBitmap PreviewImage { get => Get<WriteableBitmap>(); set => Set(value); }
    public bool ShowRefreshPreview { get => Get<bool>(); set => Set(value); }
    public string PreviewError { get => Get<string>(); set => Set(value); }

    // Animation Settings
    public int RepeatCount { get => Get(_preset.RepeatCount); set => Set(_preset.RepeatCount = value); }
    public bool EndlessLoop { get => Get(_preset.RepeatCount <= 0); set => Set(value); }
    public bool PingPong { get => Get(_preset.RepeatCount < 0); set => Set(value); }
    public bool AllowDeltaFrames { get => Get(_preset.AllowDeltaFrames); set => Set(_preset.AllowDeltaFrames = value); }
    public bool AllowClippedFrames { get => Get(_preset.AllowClippedFrames); set => Set(_preset.AllowClippedFrames = value); }
    public byte DeltaTolerance { get => Get(_preset.DeltaTolerance); set => Set(_preset.DeltaTolerance = value); }
    public bool IsAllowDeltaIgnored { get => Get<bool>(); set => Set(value); }
    public bool IsAllowClippedIgnored { get => Get<bool>(); set => Set(value); }
    public bool IsHighTolerance { get => Get<bool>(); set => Set(value); }

    #endregion

    #region Constructors

    public KGySoftGifOptionsViewModel(KGySoftGifPreset preset)
    {
        _preset = preset;
        _lastDitherer = preset.DithererId;
    }

    #endregion

    #region Methods

    #region Internal Methods

    internal async void Apply()
    {
        AdjustNotifications();
        if (_previewBitmap != null)
            return;

        if (IsExpensivePreview())
            ShowRefreshPreview = true;
        else
            await GeneratePreviewAsync();
    }

    internal async void RefreshPreview()
    {
        ShowRefreshPreview = false;
        await GeneratePreviewAsync();
    }

    #endregion

    #region Protected Methods

    protected override async void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (IsDisposed)
            return;

        switch (e.PropertyName)
        {
            case nameof(BitLevel):
                IsCustomBitLevel = BitLevel.HasValue;
                break;
            case nameof(IsCustomBitLevel):
                if (IsCustomBitLevel)
                {
                    if (BitLevel is null or < 1 or > 8)
                        BitLevel = 5;
                }
                else
                    BitLevel = null;
                break;

            case nameof(UseDitherer):
                DithererId = e.NewValue is true ? _lastDitherer ?? Ditherers.First(d => d.Id != null).Id : null;
                break;
            case nameof(DithererId):
                _lastDitherer = _preset.DithererId ?? _lastDitherer;
                UseDitherer = _preset.DithererId != null;
                break;

            case nameof(CurrentFramePath):
                if (ShowCurrentFrame)
                    await UpdateCurrentFrameAsync();
                break;
            case nameof(ShowCurrentFrame):
                await UpdateCurrentFrameAsync();
                break;

            case nameof(RepeatCount):
                EndlessLoop = e.NewValue is <= 0;
                PingPong = e.NewValue is < 0;
                break;

            case nameof(EndlessLoop):
                if (e.NewValue is true)
                    RepeatCount = 0;
                else if (RepeatCount <= 0)
                    RepeatCount = 1;
                break;

            case nameof(PingPong):
                if (e.NewValue is true)
                    RepeatCount = -1;
                break;
        }

        // As there are some awaits among the cases above we need to re-check if we are already disposed.
        if (IsDisposed)
            return;

        if (_affectsNotifications.Contains(e.PropertyName))
            AdjustNotifications();

        if (_affectsPreview.Contains(e.PropertyName) || e.PropertyName == nameof(CurrentFramePath) && ShowCurrentFrame)
        {
            if (PreviewError != null)
                return;

            if (IsExpensivePreview())
            {
                PreviewImage = null;
                ShowRefreshPreview = true;
            }
            else
                await GeneratePreviewAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;
        if (disposing)
        {
            // Canceling possible pending task but not awaiting it in Dispose, which is intended. GetAwaiter is just to suppress CS4014.
            CancelRunningGenerate();
            WaitForPendingGenerate().GetAwaiter();
            _previewBitmap = null;
            _currentFrame?.Dispose();
        }

        base.Dispose(disposing);
    }

    #endregion

    #region Private Methods

    private void AdjustNotifications()
    {
        var hasAlpha = QuantizerDescriptor.GetById(QuantizerId)?.HasAlphaThreshold is true && AlphaThreshold > 0;
        IsAllowDeltaIgnored = !hasAlpha && AllowDeltaFrames && !AllowClippedFrames;
        IsAllowClippedIgnored = !hasAlpha && !AllowDeltaFrames && AllowClippedFrames;
        IsHighTolerance = DeltaTolerance > 64;
    }

    private async Task UpdateCurrentFrameAsync()
    {
        while (_currentFrame != null)
        {
            var currentFrame = _currentFrame;
            CancelRunningGenerate();
            await WaitForPendingGenerate();

            _currentFrame = null;
            currentFrame.Dispose();
            if (IsDisposed)
                return;
        }

        // Note: we could use WPF images to open current frame: new WriteableBitmap(new BitmapImage(new Uri(CurrentFramePath))).GetReadWriteBitmapData();
        // but it has serious drawbacks:
        // - It copies the pixels one more time (BitmapImage->WriteableBitmap because WriteableBitmap cannot be created from an image file directly)
        // - In WPF nothing is disposable so we can't get rid of the temporarily allocated memory immediately
        // Therefore we use a disposable GDI+ Bitmap to create a managed clone of the image as simply as possible
        using (var bmp = ShowCurrentFrame && CurrentFramePath != null ? new Bitmap(CurrentFramePath) : Icons.Shield.ExtractBitmap(new Size(256, 256)))
        using (var nativeBitmapData = bmp.GetReadableBitmapData())
            _currentFrame = nativeBitmapData.Clone(nativeBitmapData.PixelFormat.ToKnownPixelFormat());

        // Since WriteableBitmap is not disposable we try to re-use it as much as possible.
        // It is nullified only when the resolution changes (as frames have the same resolution it happens only when toggling built-in/current frame preview)
        if (_previewBitmap != null && (_previewBitmap.PixelWidth != _currentFrame.Width || _previewBitmap.PixelHeight != _currentFrame.Height))
            _previewBitmap = null;
    }

    private async Task GeneratePreviewAsync()
    {
        // Considering that the caller method is async void this is basically a fire-and-forget operation.
        // To avoid parallel generating tasks we cancel the lastly launched possibly unfinished process.
        CancelRunningGenerate();
        Debug.Assert(_cancelGeneratingPreview == null);

        if (_currentFrame == null)
        {
            await UpdateCurrentFrameAsync();
            if (_currentFrame == null)
                return;
        }

        // Awaiting the possibly unfinished canceled task. Now it should finish quickly.
        await WaitForPendingGenerate();

        // Workaround: The awaits make this method reentrant, and a continuation can be spawn after any await at any time.
        // Therefore it is possible that despite of the line above _generatePreviewTask is not null here.
        // It could not happen after a synchronous Wait but that could cause a slight lagging (only for a short time because the task is already canceled here)
        while (_generatePreviewTask != null)
        {
            Debug.Assert(_cancelGeneratingPreview != null, "A new task is not expected to be spawned without a new cancellation");
            CancelRunningGenerate();
            await WaitForPendingGenerate();
        }

        Debug.Assert(_generatePreviewTask == null);
        if (IsDisposed)
            return;

        // We don't care about DPI here, the preview is stretched anyway.
        // The instance is created only for the first time or when resolution changes (eg. when toggling built-in/current frame preview)
        _previewBitmap ??= new WriteableBitmap(_currentFrame.Width, _currentFrame.Height, 96, 96, PixelFormats.Pbgra32, null);

        // Storing the task and cancellation source to a field so it can be canceled/awaited on reentering, disposing, etc.
        var tokenSource = _cancelGeneratingPreview = new CancellationTokenSource();
        var token = tokenSource.Token;

        ShowRefreshPreview = false;
        PreviewError = null;
        IsGenerating = true;
        var bitmapData = _previewBitmap.GetReadWriteBitmapData();
        try
        {
            // Awaiting just because of the UI thread continuation below.
            // The caller method itself is async void so it cannot be actually awaited but storing the task as a field (see also the comments above)
            await (_generatePreviewTask = CreateGenerateTask(bitmapData, token));
        }
        catch (Exception e)
        {
            if (IsDisposed || token.IsCancellationRequested)
                return;

            LogWriter.Log(e, "Failed to generate preview.");
            PreviewImage = null;
            PreviewError = LocalizationHelper.GetWithFormat("S.SaveAs.KGySoft.Preview.Error", "Failed to generate preview: {0}", e.Message);
            return;
        }
        finally
        {
            bitmapData.Dispose();
            if (!IsDisposed)
                IsGenerating = false;
        }

        if (token.IsCancellationRequested || ShowRefreshPreview)
            return;

        // Triggering preview update. Since we try to reuse always the same WriteableBitmap instance it might need a little trick:
        if (ReferenceEquals(PreviewImage, _previewBitmap))
            OnPropertyChanged(new PropertyChangedExtendedEventArgs(null, _previewBitmap, nameof(PreviewImage)));
        else
            PreviewImage = _previewBitmap;
    }

    private Task CreateGenerateTask(IReadWriteBitmapData bitmapData, CancellationToken cancellationToken) => _currentFrame.CopyToAsync(
            bitmapData,
            new Rectangle(Point.Empty, new Size(_currentFrame.Width, _currentFrame.Height)),
            Point.Empty,
            QuantizerDescriptor.Create(QuantizerId, _preset),
            DithererDescriptor.Create(DithererId, _preset),
            new TaskConfig { CancellationToken = cancellationToken, ThrowIfCanceled = false });

    private void CancelRunningGenerate()
    {
        var tokenSource = _cancelGeneratingPreview;
        if (tokenSource == null)
            return;
        tokenSource.Cancel();
        tokenSource.Dispose();
        _cancelGeneratingPreview = null;
    }

    private async Task WaitForPendingGenerate()
    {
        var runningTask = _generatePreviewTask;
        if (runningTask == null)
            return;

        Debug.Assert(_cancelGeneratingPreview == null, "Only already canceled tasks are expected to be awaited here");
        _generatePreviewTask = null;

        try
        {
            await runningTask;
        }
        catch (Exception)
        {
            // pending generate is always awaited after cancellation so ignoring everything from here
        }
    }

    private bool IsExpensivePreview() => BitLevel == 8 && QuantizerId == $"{nameof(OptimizedPaletteQuantizer)}.{nameof(OptimizedPaletteQuantizer.Wu)}";

    #endregion

    #endregion
}