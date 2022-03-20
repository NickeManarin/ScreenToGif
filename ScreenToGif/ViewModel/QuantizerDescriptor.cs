#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using KGySoft.Drawing.Imaging;
using KGySoft.Reflection;

using ScreenToGif.Util;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;

#endregion

namespace ScreenToGif.ViewModel;

/// <summary>
/// Represents a read-only descriptor ViewModel for a quantizer.
/// In fact, would be better as a nested class in <see cref="KGySoftGifOptionsViewModel"/> but WPF binding does not tolerate that.
/// </summary>
public class QuantizerDescriptor
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

    private static readonly Dictionary<string, QuantizerDescriptor> _quantizersById = _quantizers.ToDictionary(d => d.Id);

    #endregion

    #region Instance Fields

    private readonly MethodAccessor _method;
    private readonly ParameterInfo[] _parameters;

    #endregion

    #endregion

    #region Properties

    #region Static Properties

    internal static QuantizerDescriptor[] Quantizers => _quantizers;

    #endregion

    #region Instance Properties

    public string Id { get; }
    public string Title => LocalizationHelper.Get($"S.SaveAs.KGySoft.Quantizer.{Id}");
    public string Description => LocalizationHelper.Get($"S.SaveAs.KGySoft.Quantizer.{Id}.Info");
    public bool HasAlphaThreshold { get; }
    public bool HasWhiteThreshold { get; }
    public bool HasDirectMapping { get; }
    public bool HasMaxColors { get; }
    public bool HasBitLevel { get; }

    #endregion

    #endregion

    #region Constructors

    private QuantizerDescriptor(Type type, string methodName) : this(type.GetMethod(methodName))
    {}

    private QuantizerDescriptor(MethodInfo method)
    {
        _method = MethodAccessor.GetAccessor(method);
        Id = $"{method.DeclaringType.Name}.{method.Name}";
        _parameters = method.GetParameters();
        HasAlphaThreshold = _parameters.Any(p => p.Name == "alphaThreshold");
        HasWhiteThreshold = _parameters.Any(p => p.Name == "whiteThreshold");
        HasDirectMapping = _parameters.Any(p => p.Name == "directMapping");
        HasMaxColors = _parameters.Any(p => p.Name == "maxColors");
        HasBitLevel = method.DeclaringType == typeof(OptimizedPaletteQuantizer);
    }

    #endregion

    #region Methods

    internal static QuantizerDescriptor GetById(string id) => _quantizersById.GetValueOrDefault(id);

    internal static IQuantizer Create(string id, KGySoftGifPreset preset)
    {
        var descriptor = _quantizersById.GetValueOrDefault(id ?? _quantizers[0].Id) ?? throw new ArgumentException($"Invalid {id}", nameof(id));

        var args = new object[descriptor._parameters.Length];
        for (var i = 0; i < descriptor._parameters.Length; i++)
        {
            switch (descriptor._parameters[i].Name)
            {
                case "backColor":
                    args[i] = preset.BackColor.ToDrawingColor();
                    break;
                case "alphaThreshold":
                    args[i] = preset.AlphaThreshold;
                    break;
                case "whiteThreshold":
                    args[i] = preset.WhiteThreshold;
                    break;
                case "directMapping":
                    args[i] = preset.DirectMapping;
                    break;
                case "maxColors":
                    args[i] = preset.PaletteSize;
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected parameter: {descriptor._parameters[i]}");
            }
        }

        var result = (IQuantizer)descriptor._method.Invoke(null, args);

        if (result is OptimizedPaletteQuantizer opt && preset.BitLevel != 0)
            result = opt.ConfigureBitLevel(preset.BitLevel);

        return result;
    }

    #endregion
}