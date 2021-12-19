#region Usings

using System;
using System.Linq;
using System.Reflection;

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
        #region Properties

        public string Id { get; }
        public string Title => LocalizationHelper.Get($"S.SaveAs.KGySoft.Quantizer.{Id}");
        public string Description => LocalizationHelper.Get($"S.SaveAs.KGySoft.Quantizer.{Id}.Info");
        public string ImageId => "Vector.KGySoft";
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
            Id = $"{method.DeclaringType.Name}.{method.Name}";
            ParameterInfo[] methodParams = method.GetParameters();
            HasAlphaThreshold = methodParams.Any(p => p.Name == "alphaThreshold");
            HasWhiteThreshold = methodParams.Any(p => p.Name == "whiteThreshold");
            HasDirectMapping = methodParams.Any(p => p.Name == "directMapping");
            HasMaxColors = methodParams.Any(p => p.Name == "maxColors");
        }

        #endregion
    }
}