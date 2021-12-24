#region Usings

using System;
using System.Linq;
using System.Reflection;

using KGySoft.Drawing.Imaging;
using KGySoft.Reflection;

using ScreenToGif.Model.ExportPresets.AnimatedImage.Gif;
using ScreenToGif.Util;

#endregion

namespace ScreenToGif.ViewModel
{
    /// <summary>
    /// Represents a read-only descriptor ViewModel for a quantizer.
    /// In fact, would be better as a nested class in <see cref="KGySoftGifOptionsViewModel"/> but WPF binding does not tolerate that.
    /// </summary>
    public class QuantizerDescriptor
    {
        #region Fields

        private readonly MethodAccessor _method;
        private readonly ParameterInfo[] _parameters;

        #endregion

        #region Properties

        public string Id { get; }
        public string Title => LocalizationHelper.Get($"S.SaveAs.KGySoft.Quantizer.{Id}");
        public string Description => LocalizationHelper.Get($"S.SaveAs.KGySoft.Quantizer.{Id}.Info");
        public bool HasAlphaThreshold { get; }
        public bool HasWhiteThreshold { get; }
        public bool HasDirectMapping { get; }
        public bool HasMaxColors { get; }

        #endregion

        #region Constructors

        internal QuantizerDescriptor(Type type, string methodName) : this(type.GetMethod(methodName))
        {
        }

        internal QuantizerDescriptor(MethodInfo method)
        {
            _method = MethodAccessor.GetAccessor(method);
            Id = $"{method.DeclaringType.Name}.{method.Name}";
            _parameters = method.GetParameters();
            HasAlphaThreshold = _parameters.Any(p => p.Name == "alphaThreshold");
            HasWhiteThreshold = _parameters.Any(p => p.Name == "whiteThreshold");
            HasDirectMapping = _parameters.Any(p => p.Name == "directMapping");
            HasMaxColors = _parameters.Any(p => p.Name == "maxColors");
        }

        #endregion

        #region Methods

        internal IQuantizer Create(KGySoftGifPreset preset)
        {
            object[] args = new object[_parameters.Length];
            for (int i = 0; i < _parameters.Length; i++)
            {
                switch (_parameters[i].Name)
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
                        throw new InvalidOperationException($"Unexpected parameter: {_parameters[i]}");
                }
            }

            return (IQuantizer)_method.Invoke(null, args);
        }

        #endregion
    }
}