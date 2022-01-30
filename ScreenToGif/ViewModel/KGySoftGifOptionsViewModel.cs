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

using ScreenToGif.Util;
using ScreenToGif.Util.Extensions;
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

    #endregion

    #region Instance Fields

    private KGySoftGifPreset _preset;
    private string _lastDitherer;
    private WriteableBitmap _previewBitmap;
    private IReadableBitmapData _currentFrame;
    private CancellationTokenSource _cancelGeneratingPreview;
    private Task _generatePreviewTask;

    #endregion

    #endregion

    #region Properties

    // Quantizer
    public QuantizerDescriptor[] Quantizers => QuantizerDescriptor.Quantizers;
    public string QuantizerId { get => Get(_preset.QuantizerId ?? QuantizerDescriptor.Quantizers[0].Id); set => Set(value); }
    public Color BackColor { get => Get(_preset.BackColor); set => Set(value); }
    public byte AlphaThreshold { get => Get(_preset.AlphaThreshold); set => Set(value); }
    public byte WhiteThreshold { get => Get(_preset.WhiteThreshold); set => Set(value); }
    public bool DirectMapping { get => Get(_preset.DirectMapping); set => Set(value); }
    public int PaletteSize { get => Get(_preset.PaletteSize); set => Set(value); }
    public string CurrentFramePath { get => Get<string>(); set => Set(value); }
    public bool ShowCurrentFrame { get => Get(_preset.PreviewCurrentFrame); set => Set(value); }
    public byte? BitLevel { get => Get(_preset.BitLevel); set => Set(value); }
    public bool IsCustomBitLevel { get => Get<bool>(); set => Set(value); }

    // Ditherer
    public bool UseDitherer { get => Get(_preset.DithererId != null); set => Set(value); }
    public DithererDescriptor[] Ditherers => DithererDescriptor.Ditherers;
    public string DithererId { get => Get(_preset.DithererId); set => Set(value); }
    public float Strength { get => Get(_preset.Strength); set => Set(value); }
    public int? Seed { get => Get(_preset.Seed); set => Set(value); }
    public bool IsSerpentineProcessing { get => Get(_preset.IsSerpentineProcessing); set => Set(value); }

    // Preview
    public bool IsGenerating { get => Get<bool>(); set => Set(value); }
    public WriteableBitmap PreviewImage { get => Get<WriteableBitmap>(); set => Set(value); }

    #endregion

    #region Constructors

    public KGySoftGifOptionsViewModel(KGySoftGifPreset preset)
    {
        _preset = preset;
        _lastDitherer = preset.DithererId;
        IsCustomBitLevel = preset.BitLevel.HasValue;
    }

    #endregion

    #region Methods

    #region Protected Methods

    protected override async void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (IsDisposed)
            return;

        switch (e.PropertyName)
        {
            case nameof(QuantizerId):
                _preset.QuantizerId = QuantizerId;
                break;
            case nameof(BackColor):
                _preset.BackColor = BackColor;
                break;
            case nameof(AlphaThreshold):
                _preset.AlphaThreshold = AlphaThreshold;
                break;
            case nameof(WhiteThreshold):
                _preset.WhiteThreshold = WhiteThreshold;
                break;
            case nameof(DirectMapping):
                _preset.DirectMapping = DirectMapping;
                break;
            case nameof(PaletteSize):
                _preset.PaletteSize = PaletteSize;
                break;
            case nameof(BitLevel):
                _preset.BitLevel = BitLevel;
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
                _preset.DithererId = DithererId;
                _lastDitherer = _preset.DithererId ?? _lastDitherer;
                UseDitherer = _preset.DithererId != null;
                break;
            case nameof(Strength):
                _preset.Strength = Strength;
                break;
            case nameof(Seed):
                _preset.Seed = Seed;
                break;
            case nameof(IsSerpentineProcessing):
                _preset.IsSerpentineProcessing = IsSerpentineProcessing;
                break;

            case nameof(CurrentFramePath):
                if (ShowCurrentFrame)
                    await UpdateCurrentFrameAsync();
                break;

            case nameof(ShowCurrentFrame):
                await UpdateCurrentFrameAsync();
                break;
        }

        if (IsDisposed)
            return;
        if (_affectsPreview.Contains(e.PropertyName) || e.PropertyName == nameof(CurrentFramePath) && (ShowCurrentFrame || _previewBitmap == null))
            await GeneratePreviewAsync();
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
            _currentFrame = nativeBitmapData.Clone(nativeBitmapData.PixelFormat);

        // Since WritebleBitmap is not disposable we try to re-use it as much as possible.
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
        // It could not happen after a synchronous Wait but that could cause a slight lagging (for a short period because the task is already canceled here)
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

        // Storing the task and cancellation source to a field so it can be canceled/awaited on re-entrancing, disposing, etc.
        var tokenSource = _cancelGeneratingPreview = new CancellationTokenSource();
        CancellationToken token = tokenSource.Token;

        IsGenerating = true;
        IReadWriteBitmapData bitmapData = _previewBitmap.GetReadWriteBitmapData();
        try
        {
            // Awaiting just because of the UI thread continuation below.
            // The caller method itself is async void so it cannot be awaited but storing the task as a field (see also the comments above)
            await (_generatePreviewTask = CreateGenerateTask(bitmapData, token));
        }
        catch (Exception e)
        {
            if (IsDisposed)
                return;
            if (!token.IsCancellationRequested)
            {
                LogWriter.Log(e, "Failed to generate preview.");
                    PreviewImage = null;
            }
        }
        finally
        {
            bitmapData.Dispose();
            if (!IsDisposed)
                IsGenerating = false;
        }

        if (token.IsCancellationRequested)
            return;

        PreviewImage = _previewBitmap;
        OnPropertyChanged(new PropertyChangedExtendedEventArgs(null, _previewBitmap, nameof(PreviewImage)));
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

    #endregion

    #endregion
}