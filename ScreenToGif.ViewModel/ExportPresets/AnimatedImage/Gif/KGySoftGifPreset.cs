#region Usings

using System.Windows.Media;

using KGySoft.Drawing.Imaging;

using ScreenToGif.Domain.Enums;

#endregion

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;

/// <summary>
/// Represents the persistable settings for KGy SOFT GIF Encoder.
/// </summary>
public class KGySoftGifPreset : GifPreset
{
    #region Fields

    private string _quantizerId;
    private string _dithererId;
    private Color _backColor = Colors.Silver;
    private byte _alphaThreshold = 128;
    private byte _whiteThreshold = 128;
    private bool _directMapping;
    private int _paletteSize = 256;
    private byte? _bitLevel;
    private float _strength;
    private int? _seed;
    private bool _serpentine;
    private bool _allowDeltaFrames = true;
    private bool _allowClippedFrames = true;
    private byte _deltaTolerance;

    #endregion

    #region Properties

    #region Static Properties

    /// <summary>
    /// Gets the defaults presets for the <see cref="KGySoftGifPreset"/> type.
    /// </summary>
    public static KGySoftGifPreset[] Defaults => new[]
    {
        new KGySoftGifPreset
        {
            TitleKey = "S.Preset.Gif.KGySoft.Balanced.Title",
            DescriptionKey = "S.Preset.Gif.KGySoft.Balanced.Description",
            HasAutoSave = true,
            IsSelected = true,
            IsDefault = true,
            IsSelectedForEncoder = true,
            CreationDate = new DateTime(2021, 12, 15),
            QuantizerId = $"{nameof(OptimizedPaletteQuantizer)}.{nameof(OptimizedPaletteQuantizer.Wu)}",
        },
        new KGySoftGifPreset
        {
            TitleKey = "S.Preset.Gif.KGySoft.High.Title",
            DescriptionKey = "S.Preset.Gif.KGySoft.High.Description",
            HasAutoSave = true,
            IsSelected = true,
            IsDefault = true,
            IsSelectedForEncoder = true,
            CreationDate = new DateTime(2021, 12, 15),
            QuantizerId = $"{nameof(OptimizedPaletteQuantizer)}.{nameof(OptimizedPaletteQuantizer.Wu)}",
            DithererId = $"{nameof(ErrorDiffusionDitherer)}.{nameof(ErrorDiffusionDitherer.FloydSteinberg)}",
            BitLevel = 7,
        },
        new KGySoftGifPreset
        {
            TitleKey = "S.Preset.Gif.KGySoft.Fast.Title",
            DescriptionKey = "S.Preset.Gif.KGySoft.Fast.Description",
            HasAutoSave = true,
            IsSelected = true,
            IsDefault = true,
            IsSelectedForEncoder = true,
            CreationDate = new DateTime(2021, 12, 15),
            QuantizerId = $"{nameof(PredefinedColorsQuantizer)}.{nameof(PredefinedColorsQuantizer.SystemDefault8BppPalette)}",
            DithererId = $"{nameof(OrderedDitherer)}.{nameof(OrderedDitherer.Bayer8x8)}",
        },
   };

    #endregion

    #region Instance Properties

    #region Quantizer Settings

    /// <summary>
    /// Gets or sets the quantizer identifier in {TypeName}.{MethodName} format.
    /// </summary>
    public string QuantizerId
    {
        get => _quantizerId;
        set => SetProperty(ref _quantizerId, value);
    }

    /// <summary>
    /// Gets or sets the background color for alpha pixels that will not be transparent in the result.
    /// </summary>
    public Color BackColor
    {
        get => _backColor;
        set => SetProperty(ref _backColor, value);
    }

    /// <summary>
    /// Gets or sets the alpha threshold under which a color is considered transparent.
    /// This property is ignored by quantizers that do not support transparency.
    /// </summary>
    public byte AlphaThreshold
    {
        get => _alphaThreshold;
        set => SetProperty(ref _alphaThreshold, value);
    }

    /// <summary>
    /// Gets or sets the lowest input brightness to consider the result color white.
    /// This property is considered only by the black and white quantizer.
    /// </summary>
    public byte WhiteThreshold
    {
        get => _whiteThreshold;
        set => SetProperty(ref _whiteThreshold, value);
    }

    /// <summary>
    /// Gets or sets whether the palette entries are mapped from the color directly.
    /// This property is ignored by quantizers that do not support direct mapping.
    /// </summary>
    public bool DirectMapping
    {
        get => _directMapping;
        set => SetProperty(ref _directMapping, value);
    }

    /// <summary>
    /// Gets or sets the maximum palette size per frame.
    /// This property is ignored by predefined colors quantizers.
    /// </summary>
    public int PaletteSize
    {
        get => _paletteSize;
        set => SetProperty(ref _paletteSize, value);
    }

    /// <summary>
    /// Gets or sets the bit level used by an optimized quantizer.
    /// This property is ignored by predefined colors quantizers.
    /// </summary>
    public byte? BitLevel
    {
        get => _bitLevel;
        set => SetProperty(ref _bitLevel, value);
    }

    #endregion

    #region Ditherer Settings

    /// <summary>
    /// Gets or sets the ditherer identifier in {TypeName}[.{PropertyName}] format.
    /// </summary>
    public string DithererId
    {
        get => _dithererId;
        set => SetProperty(ref _dithererId, value);
    }

    /// <summary>
    /// Gets or sets the strength of the ditherer.
    /// This property is ignored by error diffusion ditherers.
    /// </summary>
    public float Strength
    {
        get => _strength;
        set => SetProperty(ref _strength, value);
    }

    /// <summary>
    /// Gets or sets the seed of ditherer.
    /// This property is ignored by non-randomized ditherers.
    /// </summary>
    public int? Seed
    {
        get => _seed;
        set => SetProperty(ref _seed, value);
    }

    /// <summary>
    /// Gets or sets whether the ditherer uses serpentine processing.
    /// This property is used only by error diffusion ditherers.
    /// </summary>
    public bool IsSerpentineProcessing
    {
        get => _serpentine;
        set => SetProperty(ref _serpentine, value);
    }

    #endregion

    #region Animation Settings

    /// <summary>
    /// Gets or sets whether the encoder is allowed to save the changed image parts.
    /// </summary>
    public bool AllowDeltaFrames
    {
        get => _allowDeltaFrames;
        set => SetProperty(ref _allowDeltaFrames, value);
    }

    /// <summary>
    /// Gets or sets whether the encoder is allowed clip the transparent border of the frames.
    /// </summary>
    public bool AllowClippedFrames
    {
        get => _allowClippedFrames;
        set => SetProperty(ref _allowClippedFrames, value);
    }

    /// <summary>
    /// If <see cref="AllowDeltaFrames"/> is <see langword="true"/>, then gets or sets the allowed maximum tolerance when detecting changes.
    /// </summary>
    public byte DeltaTolerance
    {
        get => _deltaTolerance;
        set => SetProperty(ref _deltaTolerance, value);
    }

    #endregion

    #endregion

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new instance of the <see cref="KGySoftGifPreset"/> class.
    /// </summary>
    public KGySoftGifPreset()
    {
        Encoder = EncoderTypes.KGySoft;
        ImageId = "Vector.KGySoft";
        RepeatCount = 0;
    }

    #endregion
}
