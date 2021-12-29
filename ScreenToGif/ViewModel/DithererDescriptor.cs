#region Usings

using System;
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

    private readonly CreateInstanceAccessor _ctor;
    private readonly ParameterInfo[] _parameters;
    private readonly PropertyAccessor _property;

    #endregion

    #region Properties

    public string Id { get; }
    public string Title => LocalizationHelper.Get($"S.SaveAs.KGySoft.Ditherer.{Id ?? "None"}");
    public string Description => LocalizationHelper.Get($"S.SaveAs.KGySoft.Ditherer.{Id ?? "None"}.Info");
    public bool HasStrength { get; }
    public bool HasSeed { get; }
    public bool HasSerpentineProcessing { get; }

    #endregion

    #region Constructors

    internal DithererDescriptor(Type type, string propertyName) : this(type.GetProperty(propertyName))
    {
    }

    internal DithererDescriptor(MemberInfo member)
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

    internal IDitherer Create(KGySoftGifPreset preset)
    {
        // by constructor
        if (_ctor != null)
        {
            object[] args = new object[_parameters.Length];
            for (int i = 0; i < _parameters.Length; i++)
            {
                switch (_parameters[i].Name)
                {
                    case "strength":
                        args[i] = preset.Strength;
                        break;
                    case "seed":
                        args[i] = preset.Seed;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected parameter: {_parameters[i]}");
                }
            }

            return (IDitherer)_ctor.CreateInstance(args);
        }

        // by property
        Debug.Assert(_property != null);
        IDitherer result = (IDitherer)_property.Get(null);
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