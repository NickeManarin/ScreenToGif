#region Usings

#region Used Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
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

#nullable enable

namespace ScreenToGif.ViewModel;

/// <summary>
/// Provides the ViewModel class for the <see cref="KGySoftGifPreset"/> model type.
/// </summary>
public class KGySoftGifOptionsViewModel : ObservableObjectBase
{
    #region Nested Types

    private record Configuration // as a record, so equality check compares the properties
    {
        #region Properties

        internal IReadableBitmapData? Frame { get; private init; }
        internal string? QuantizerId { get; private init; }
        internal string? DithererId { get; private init; }
        internal Color BackColor { get; private init; }
        internal byte AlphaThreshold { get; private init; }
        internal byte WhiteThreshold { get; private init; }
        internal bool DirectMapping { get; private init; }
        internal int PaletteSize { get; private init; }
        internal byte? BitLevel { get; private init; }
        internal bool LinearColorSpace { get; private init; }
        internal float Strength { get; private init; }
        internal int? Seed { get; private init; }
        internal bool IsSerpentineProcessing { get; private init; }

        #endregion

        #region Methods

        internal static Configuration Capture(KGySoftGifOptionsViewModel viewModel) => new Configuration
        {
            Frame = viewModel._currentFrame,
            QuantizerId = viewModel.QuantizerId,
            DithererId = viewModel.DithererId,
            BackColor = viewModel.BackColor,
            AlphaThreshold = viewModel.AlphaThreshold,
            WhiteThreshold = viewModel.WhiteThreshold,
            DirectMapping = viewModel.DirectMapping,
            PaletteSize = viewModel.PaletteSize,
            BitLevel = viewModel.BitLevel,
            LinearColorSpace = viewModel.LinearColorSpace,
            Strength = viewModel.Strength,
            Seed = viewModel.Seed,
            IsSerpentineProcessing = viewModel.IsSerpentineProcessing,
        };

        #endregion

        #region Methods

        // NOTE: Cannot use KGySoftGifOptionsViewModel._preset because it mutates the same instance,
        // and the result should not be a cached in a field either, because it would be included in record equality.
        internal KGySoftGifPreset GetPreset() => new KGySoftGifPreset
        {
            QuantizerId = QuantizerId,
            DithererId = DithererId,
            BackColor = BackColor,
            AlphaThreshold = AlphaThreshold,
            WhiteThreshold = WhiteThreshold,
            DirectMapping = DirectMapping,
            PaletteSize = PaletteSize,
            BitLevel = BitLevel,
            LinearColorSpace = LinearColorSpace,
            Strength = Strength,
            Seed = Seed,
            IsSerpentineProcessing = IsSerpentineProcessing
        };

        #endregion
    }

    #endregion

    #region Fields

    #region Static Fields

    private static readonly HashSet<string?> _affectsPreview =
    [
        nameof(QuantizerId),
        nameof(BackColor),
        nameof(AlphaThreshold),
        nameof(WhiteThreshold),
        nameof(DirectMapping),
        nameof(PaletteSize),
        nameof(BitLevel),
        nameof(LinearColorSpace),

        // ditherer settings
        nameof(DithererId),
        nameof(Strength),
        nameof(Seed),
        nameof(IsSerpentineProcessing),

        // preview settings
        nameof(ShowCurrentFrame)
    ];

    private static readonly HashSet<string?> _affectsNotifications =
    [
        nameof(QuantizerId),
        nameof(AlphaThreshold),
        nameof(AllowDeltaFrames),
        nameof(AllowClippedFrames),
        nameof(DeltaTolerance)
    ];
    
    private static bool _lastShowCurrentFrame = true;
    private static string? _lastDitherer;

    #endregion

    #region Instance Fields

    private readonly KGySoftGifPreset _preset;
    private readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);

    private WriteableBitmap? _previewBitmap;
    private IReadableBitmapData? _currentFrame;
    private CancellationTokenSource? _cancelGeneratingPreview;
    private Task? _generatePreviewTask;

    #endregion

    #endregion

    #region Properties

    // Quantizer
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Used by WPF binding")]
    public QuantizerDescriptor[] Quantizers => QuantizerDescriptor.Quantizers;
    public string QuantizerId { get => Get(_preset.QuantizerId ?? QuantizerDescriptor.Quantizers[0].Id); set => Set(_preset.QuantizerId = value); }
    public Color BackColor { get => Get(_preset.BackColor); set => Set(_preset.BackColor = value); }
    public byte AlphaThreshold { get => Get(_preset.AlphaThreshold); set => Set(_preset.AlphaThreshold = value); }
    public byte WhiteThreshold { get => Get(_preset.WhiteThreshold); set => Set(_preset.WhiteThreshold = value); }
    public bool DirectMapping { get => Get(_preset.DirectMapping); set => Set(_preset.DirectMapping = value); }
    public int PaletteSize { get => Get(_preset.PaletteSize); set => Set(_preset.PaletteSize = value); }
    public byte? BitLevel { get => Get(_preset.BitLevel); set => Set(_preset.BitLevel = value); }
    public bool IsCustomBitLevel { get => Get(_preset.BitLevel.HasValue); set => Set(value); }
    public bool LinearColorSpace { get => Get(_preset.LinearColorSpace); set => Set(_preset.LinearColorSpace = value); }

    // Ditherer
    public bool UseDitherer { get => Get(_preset.DithererId != null); set => Set(value); }
    public DithererDescriptor[] Ditherers => DithererDescriptor.Ditherers;
    public string? DithererId { get => Get(_preset.DithererId); set => Set(_preset.DithererId = value); }
    public float Strength { get => Get(_preset.Strength); set => Set(_preset.Strength = value); }
    public int? Seed { get => Get(_preset.Seed); set => Set(_preset.Seed = value); }
    public bool IsSerpentineProcessing { get => Get(_preset.IsSerpentineProcessing); set => Set(_preset.IsSerpentineProcessing = value); }

    // Preview
    public bool ShowCurrentFrame { get => Get(_lastShowCurrentFrame); set => Set(_lastShowCurrentFrame = value); }
    public string? CurrentFramePath { get => Get<string?>(); set => Set(value); }
    public bool IsGenerating { get => Get<bool>(); set => Set(value); }
    public WriteableBitmap? PreviewImage { get => Get<WriteableBitmap>(); set => Set(value); }
    public bool ShowRefreshPreview { get => Get<bool>(); set => Set(value); }
    public string? PreviewError { get => Get<string?>(); set => Set(value); }

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

    internal async Task Apply()
    {
        AdjustNotifications();
        if (_previewBitmap != null)
            return;

        if (IsExpensivePreview())
        {
            ShowRefreshPreview = true;
            return;
        }

        await EnsureCurrentFrame();
        await GeneratePreviewAsync(Configuration.Capture(this));
    }

    internal async Task RefreshPreview()
    {
        ShowRefreshPreview = false;
        await EnsureCurrentFrame();
        await GeneratePreviewAsync(Configuration.Capture(this));
    }

    #endregion

    #region Protected Methods

    [SuppressMessage("ReSharper", "AsyncVoidEventHandlerMethod", Justification = "Overridden event handler method.")]
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
                return;
            }

            await EnsureCurrentFrame();
            await GeneratePreviewAsync(Configuration.Capture(this));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        // Canceling possible pending task but not awaiting it in Dispose, which is intended. GetAwaiter is just to suppress CS4014.
        if (disposing)
            CancelAndAwaitPendingGenerate().GetAwaiter();

        base.Dispose(disposing);
        if (disposing)
        {
            _previewBitmap = null;
            _currentFrame?.Dispose();
            _cancelGeneratingPreview?.Dispose();
            _syncRoot.Dispose();
        }
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
            await CancelAndAwaitPendingGenerate();

            _currentFrame = null;
            currentFrame.Dispose();
            if (IsDisposed)
                return;
        }

        if (CurrentFramePath == null || !Path.Exists(CurrentFramePath))
        {
            _previewBitmap = null;
            PreviewImage = _previewBitmap;
            return;
        }

        // Note: we could use WPF images to open current frame: new WriteableBitmap(new BitmapImage(new Uri(CurrentFramePath))).GetReadWriteBitmapData();
        // but it has serious drawbacks:
        // - It copies the pixels one more time (BitmapImage->WriteableBitmap because WriteableBitmap cannot be created from an image file directly)
        // - In WPF nothing is disposable (not even bitmaps, which wrap unmanaged handles) so we can't get rid of the temporarily allocated memory immediately
        // Therefore, we use a disposable GDI+ Bitmap to create a managed clone of the image as simply as possible
        try
        {
            using var bmp = ShowCurrentFrame ? new Bitmap(CurrentFramePath) : Icons.Shield.ExtractBitmap(new Size(256, 256))!;
            using var nativeBitmapData = bmp.GetReadableBitmapData();
            _currentFrame = (await nativeBitmapData.CloneAsync(nativeBitmapData.PixelFormat.ToKnownPixelFormat()))!; // will not be null, because it is never canceled
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Failed to update current frame.");
            _currentFrame = null;
            _previewBitmap = null;
            return;
        }

        // Since WriteableBitmap is not disposable we try to re-use it as much as possible.
        // It is nullified only when the resolution changes (as frames have the same resolution it happens only when toggling built-in/current frame preview)
        if (_previewBitmap != null && (_previewBitmap.PixelWidth != _currentFrame.Width || _previewBitmap.PixelHeight != _currentFrame.Height))
            _previewBitmap = null;
    }

    private async Task EnsureCurrentFrame()
    {
        if (_currentFrame == null)
            await UpdateCurrentFrameAsync();
    }

    private async Task GeneratePreviewAsync(Configuration cfg)
    {
        if (IsDisposed || cfg.Frame == null)
            return;

        // Using a while instead of an if, because the awaits make this method reentrant, and a continuation can be spawn after any await at any time.
        // Therefore, it is possible that though we cleared _generatePreviewTask in WaitForPendingGenerate, it is not null upon starting the continuation.
        while (_generatePreviewTask != null)
            await CancelAndAwaitPendingGenerate();

        // Using a manually completable task for the generateResultTask field. If this method had just one awaitable task we could simply assign that to the field.
        TaskCompletionSource? generateTaskCompletion = null;

        // This is essentially a lock. Achieved by a SemaphoreSlim, because an actual lock cannot be used with awaits in the code.
        await _syncRoot.WaitAsync();
        try
        {
            // lost race: returning if configuration has been changed by the time we entered the lock
            if (cfg != Configuration.Capture(this) || IsDisposed)
                return;

            Debug.Assert(_cancelGeneratingPreview == null && _generatePreviewTask == null);

            // We don't care about DPI here, the preview is stretched anyway.
            // The instance is created only for the first time or when resolution changes (e.g. when toggling built-in/current frame preview)
            _previewBitmap ??= new WriteableBitmap(cfg.Frame.Width, cfg.Frame.Height, 96, 96, PixelFormats.Pbgra32, null);

            // Storing the task and cancellation source to a field, so it can be canceled/awaited on reentering, disposing, etc.
            var tokenSource = _cancelGeneratingPreview = new CancellationTokenSource();
            generateTaskCompletion = new TaskCompletionSource();
            _generatePreviewTask = generateTaskCompletion.Task;
            var token = tokenSource.Token;

            ShowRefreshPreview = false;
            PreviewError = null;
            IsGenerating = true;
            var bitmapData = _previewBitmap.GetReadWriteBitmapData();
            try
            {
                var capturedPreset = cfg.GetPreset();
                await cfg.Frame.CopyToAsync(bitmapData,
                    new Rectangle(Point.Empty, new Size(cfg.Frame.Width, cfg.Frame.Height)), Point.Empty,
                    QuantizerDescriptor.Create(cfg.QuantizerId, capturedPreset),
                    DithererDescriptor.Create(cfg.DithererId, capturedPreset),
                    new TaskConfig { CancellationToken = token, ThrowIfCanceled = false });
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
        finally
        {
            generateTaskCompletion?.SetResult();
            try
            {
                if (!IsDisposed)
                    _syncRoot.Release();
            }
            catch (ObjectDisposedException)
            {
                // there is still a small chance
            }
        }
    }

    private async Task CancelAndAwaitPendingGenerate()
    {
        #region Local Methods

        void CancelRunningGenerate()
        {
            var tokenSource = _cancelGeneratingPreview;
            if (tokenSource == null)
                return;
            tokenSource.Cancel();
            tokenSource.Dispose();
            _cancelGeneratingPreview = null;
        }

        async Task WaitForPendingGenerate()
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

        CancelRunningGenerate();
        await WaitForPendingGenerate();
    }

    private bool IsExpensivePreview() => BitLevel == 8 && QuantizerId == $"{nameof(OptimizedPaletteQuantizer)}.{nameof(OptimizedPaletteQuantizer.Wu)}";

    #endregion

    #endregion
}