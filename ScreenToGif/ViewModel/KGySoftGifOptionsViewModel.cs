#region Usings

#region Used Namespaces

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

    private static readonly HashSet<string> _affectsPreview = new()
    {
        // quantizer settings
        nameof(QuantizerId), nameof(BackColor), nameof(AlphaThreshold), nameof(WhiteThreshold), nameof(DirectMapping), nameof(PaletteSize), nameof(BitLevel),

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
    public bool IsGenerating { get=> Get<bool>(); set => Set(value); }
    public WriteableBitmap PreviewImage { get=> Get<WriteableBitmap>(); set => Set(value); }

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
            _currentFrame.CopyTo(previewBitmapData, Point.Empty, QuantizerDescriptor.Create(QuantizerId, _preset), DithererDescriptor.Create(DithererId, _preset));

        // TODO: or canceled
        if (IsDisposed)
            return;

        PreviewImage = _previewBitmap;
        OnPropertyChanged(new PropertyChangedExtendedEventArgs(null, _previewBitmap, nameof(PreviewImage)));
    }

    #endregion

    #endregion
}