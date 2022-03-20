#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using KGySoft.Drawing.Imaging;
using KGySoft.Reflection;

using ScreenToGif.Util;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;

#endregion

namespace ScreenToGif.ViewModel;

/// <summary>
/// Represents a read-only descriptor ViewModel for a ditherer.
/// In fact, would be better as a nested class in <see cref="KGySoftGifOptionsViewModel"/> but WPF binding does not tolerate that.
/// </summary>
public class DithererDescriptor
{
    #region Fields

    #region Static Fields

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

    private static readonly Dictionary<string, DithererDescriptor> _ditherersById = _ditherers.Where(d => d.Id != null).ToDictionary(d => d.Id);

    #endregion

    #region Instance Fields

    private readonly CreateInstanceAccessor _ctor;
    private readonly ParameterInfo[] _parameters;
    private readonly PropertyAccessor _property;

    #endregion

    #endregion

    #region Properties

    #region Static Properties

    internal static DithererDescriptor[] Ditherers => _ditherers;

    #endregion

    #region Instance Properties

    public string Id { get; }
    public string Title => LocalizationHelper.Get($"S.SaveAs.KGySoft.Ditherer.{Id?.Replace("x", "X") ?? "None"}");
    public string Description => LocalizationHelper.Get($"S.SaveAs.KGySoft.Ditherer.{Id?.Replace("x", "X") ?? "None"}.Info");
    public bool HasStrength { get; }
    public bool HasSeed { get; }
    public bool HasSerpentineProcessing { get; }

    #endregion

    #endregion

    #region Constructors

    private DithererDescriptor(Type type, string propertyName) : this(type.GetProperty(propertyName))
    {}

    private DithererDescriptor(MemberInfo member)
    {
        switch (member)
        {
            case ConstructorInfo ctor:
                _parameters = ctor.GetParameters();
                _ctor = CreateInstanceAccessor.GetAccessor(ctor);
                Id = ctor.DeclaringType.Name;
                HasStrength = _parameters.Any(p => p.Name == "strength");
                HasSeed = _parameters.Any(p => p.Name == "seed");
                break;

            case PropertyInfo property:
                _property = PropertyAccessor.GetAccessor(property);
                Id = $"{property.DeclaringType.Name}.{property.Name}";
                HasStrength = property.DeclaringType == typeof(OrderedDitherer);
                HasSerpentineProcessing = property.DeclaringType == typeof(ErrorDiffusionDitherer);
                break;

            case null:
                break;

            default:
                throw new ArgumentException($"Unexpected member: {member}");
        }
    }

    #endregion

    #region Methods

    internal static IDitherer Create(string id, KGySoftGifPreset preset)
    {
        if (id == null)
            return null;

        var descriptor = _ditherersById.GetValueOrDefault(id) ?? throw new ArgumentException($"Invalid {id}", nameof(id));

        //By constructor
        if (descriptor._ctor != null)
        {
            var args = new object[descriptor._parameters.Length];

            for (var i = 0; i < descriptor._parameters.Length; i++)
            {
                switch (descriptor._parameters[i].Name)
                {
                    case "strength":
                        args[i] = preset.Strength;
                        break;
                    case "seed":
                        args[i] = preset.Seed;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected parameter: {descriptor._parameters[i]}");
                }
            }

            return (IDitherer)descriptor._ctor.CreateInstance(args);
        }

        //By property
        Debug.Assert(descriptor._property != null);
        var result = (IDitherer)descriptor._property.Get(null);

        switch (result)
        {
            case OrderedDitherer ordered when preset.Strength > 0f:
                result = ordered.ConfigureStrength(preset.Strength);
                break;
            case ErrorDiffusionDitherer errorDiffusion when preset.IsSerpentineProcessing:
                result = errorDiffusion.ConfigureProcessingDirection(true);
                break;
        }

        return result;
    }

    #endregion
}