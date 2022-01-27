#region Usings

#region Used Namespaces

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;

using KGySoft.ComponentModel;
using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;

using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;
using ScreenToGif.Util.Extensions;

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

    private static readonly QuantizerDescriptor[] _quantizers =
    {
        new(typeof(PredefinedColorsQuantizer), nameof(PredefinedColorsQuantizer.BlackAndWhite)),
        new(typeof(PredefinedColorsQuantizer), nameof(PredefinedColorsQuantizer.Grayscale4)),
        new(typeof(PredefinedColorsQuantizer), nameof(PredefinedColorsQuantizer.Grayscale16)),
        new(typeof(PredefinedColorsQuantizer), nameof(PredefinedColorsQuantizer.Grayscale)),
        new(typeof(PredefinedColorsQuantizer), nameof(PredefinedColorsQuantizer.SystemDefault4BppPalette)),
        new(typeof(PredefinedColorsQuantizer), nameof(PredefinedColorsQuantizer.SystemDefault8BppPalette)),
        new(typeof(PredefinedColorsQuantizer), nameof(PredefinedColorsQuantizer.Rgb332)),

        new(typeof(OptimizedPaletteQuantizer), nameof(OptimizedPaletteQuantizer.Octree)),
        new(typeof(OptimizedPaletteQuantizer), nameof(OptimizedPaletteQuantizer.MedianCut)),
        new(typeof(OptimizedPaletteQuantizer), nameof(OptimizedPaletteQuantizer.Wu)),
    };

    private static readonly DithererDescriptor[] _ditherers =
    {
        new(null),

        new(typeof(OrderedDitherer), nameof(OrderedDitherer.Bayer2x2)),
        new(typeof(OrderedDitherer), nameof(OrderedDitherer.Bayer3x3)),
        new(typeof(OrderedDitherer), nameof(OrderedDitherer.Bayer4x4)),
        new(typeof(OrderedDitherer), nameof(OrderedDitherer.Bayer8x8)),
        new(typeof(OrderedDitherer), nameof(OrderedDitherer.DottedHalftone)),
        new(typeof(OrderedDitherer), nameof(OrderedDitherer.BlueNoise)),

        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.Atkinson)),
        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.Burkes)),
        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.FloydSteinberg)),
        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.JarvisJudiceNinke)),
        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.Sierra3)),
        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.Sierra2)),
        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.SierraLite)),
        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.StevensonArce)),
        new(typeof(ErrorDiffusionDitherer), nameof(ErrorDiffusionDitherer.Stucki)),

        new(typeof(RandomNoiseDitherer).GetConstructor(new[] { typeof(float), typeof(int?) })),
        new(typeof(InterleavedGradientNoiseDitherer).GetConstructor(new[] { typeof(float) })),
    };
        
    private static readonly Dictionary<string, QuantizerDescriptor> _quantizersById = _quantizers.ToDictionary(d => d.Id);
    private static readonly Dictionary<string, DithererDescriptor> _ditherersById = _ditherers.Where(d => d.Id != null).ToDictionary(d => d.Id);

    private static readonly HashSet<string> _affectsPreview = new()
    {
        // quantizer settings
        nameof(QuantizerId), nameof(BackColor), nameof(AlphaThreshold), nameof(WhiteThreshold), nameof(DirectMapping), nameof(PaletteSize),

        // ditherer settings
        nameof(DithererId), nameof(Strength), nameof(Seed), nameof(IsSerpentineProcessing),

        // preview settings
        nameof(ShowCurrentFrame)
    };

    #endregion

    #region Instance Fields

    private readonly KGySoftGifPreset _preset;

    private IReadableBitmapData _currentFrame;
    private WriteableBitmap _previewBitmap;
    private string _lastDitherer;

    #endregion

    #endregion

    #region Properties

    // Quantizer
    public QuantizerDescriptor[] Quantizers => _quantizers;
    public string QuantizerId { get => Get(_preset.QuantizerId ?? _quantizers[0].Id); set => Set(value); }
    public Color BackColor { get => Get(_preset.BackColor); set => Set(value); }
    public byte AlphaThreshold { get => Get(_preset.AlphaThreshold); set => Set(value); }
    public byte WhiteThreshold { get => Get(_preset.WhiteThreshold); set => Set(value); }
    public bool DirectMapping { get => Get(_preset.DirectMapping); set => Set(value); }
    public int PaletteSize { get => Get(_preset.PaletteSize); set => Set(value); }
    public string CurrentFramePath { get => Get<string>(); set => Set(value); }
    public bool ShowCurrentFrame { get => Get(_preset.PreviewCurrentFrame); set => Set(value); }

    // Ditherer
    public bool UseDitherer { get => Get(_preset.DithererId != null); set => Set(value); }
    public DithererDescriptor[] Ditherers => _ditherers;
    public string DithererId { get => Get(_preset.DithererId); set => Set(value); }
    public float Strength { get => Get(_preset.Strength); set => Set(value); }
    public int? Seed { get => Get(_preset.Seed); set => Set(value); }
    public bool IsSerpentineProcessing { get => Get(_preset.IsSerpentineProcessing); set => Set(value); }

    // Preview
    public bool IsGenerating { get=> Get<bool>(); set => Set(value); }
    public WriteableBitmap PreviewImage { get=> Get<WriteableBitmap>(); set => Set(value); }

    #endregion

    #region Constructors

    public KGySoftGifOptionsViewModel(KGySoftGifPreset preset)
    {
        _preset = preset;
        _lastDitherer = preset.DithererId;
    }

    #endregion

    #region Methods

    #region Protected Methods

    protected override void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
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

            case nameof(UseDitherer):
                DithererId = e.NewValue is true ? _lastDitherer ?? _ditherers.First(d => d.Id != null).Id : null;
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
            case nameof(ShowCurrentFrame):
                UpdateCurrentFrame();
                break;

        }

        if (_affectsPreview.Contains(e.PropertyName) || e.PropertyName == nameof(CurrentFramePath) && (ShowCurrentFrame || _previewBitmap == null))
            GeneratePreview();
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;
        _previewBitmap = null;
        _currentFrame?.Dispose();
        base.Dispose(disposing);
    }

    #endregion

    #region Private Methods

    private void UpdateCurrentFrame()
    {
        if (_currentFrame != null)
        {
            _currentFrame.Dispose();
            _currentFrame = null;
        }

        // Note: we could use WPF images to open current frame: new WriteableBitmap(new BitmapImage(new Uri(CurrentFramePath))).GetReadWriteBitmapData();
        // but it has serious drawbacks:
        // - It copied copy the pixels one more time (BitmapImage->WriteableBitmap because WriteableBitmap cannot be created from an image file directly)
        // - In WPF nothing is disposable so we can't get rid of the temporarily allocated memory immediately
        // Therefore we use a disposable GDI+ Bitmap to create a managed clone of the image as simply as possible
        using (var bmp = ShowCurrentFrame && CurrentFramePath != null ? new Bitmap(CurrentFramePath) : Icons.Shield.ExtractBitmap(new Size(256, 256)))
        using (var nativeBitmapData = bmp.GetReadableBitmapData())
            _currentFrame = nativeBitmapData.Clone(nativeBitmapData.PixelFormat);

        if (_previewBitmap != null && (_previewBitmap.PixelWidth != _currentFrame.Width || _previewBitmap.PixelHeight != _currentFrame.Height))
            _previewBitmap = null;
    }

    private void GeneratePreview()
    {
        // TODO: cancellation

        if (_currentFrame == null)
        {
            UpdateCurrentFrame();
            if (_currentFrame == null)
                return;
        }

        // We don't care about DPI here, the preview is stretched anyway
        if (_previewBitmap == null)
            _previewBitmap = new WriteableBitmap(_currentFrame.Width, _currentFrame.Height, 96, 96, PixelFormats.Pbgra32, null);

        using (IReadWriteBitmapData previewBitmapData = _previewBitmap.GetReadWriteBitmapData())
            _currentFrame.CopyTo(previewBitmapData, Point.Empty, GetQuantizer(), GetDitherer());

        // TODO: or canceled
        if (IsDisposed)
            return;

        PreviewImage = _previewBitmap;
        OnPropertyChanged(new PropertyChangedExtendedEventArgs(null, _previewBitmap, nameof(PreviewImage)));
    }

    private IQuantizer GetQuantizer()
    {
        string id = QuantizerId;
        if (id == null)
            throw new InvalidOperationException($"{nameof(QuantizerId)} is null");

        QuantizerDescriptor descriptor = _quantizersById.GetValueOrDefault(id) ?? throw new InvalidOperationException($"Invalid {QuantizerId}");
        return descriptor.Create(_preset);
    }

    private IDitherer GetDitherer()
    {
        string id = DithererId;
        if (id == null)
            return null;

        DithererDescriptor descriptor = _ditherersById.GetValueOrDefault(id) ?? throw new InvalidOperationException($"Invalid {DithererId}");
        return descriptor.Create(_preset);
    }

    #endregion

    #endregion
}