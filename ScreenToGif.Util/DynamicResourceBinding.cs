using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Binding = System.Windows.Data.Binding;

namespace ScreenToGif.Util;

/// <summary>
/// From MarqueIV: http://stackoverflow.com/questions/33816511/how-do-you-create-a-dynamicresourcebinding-that-supports-converters-stringforma
/// </summary>
public class DynamicResourceBinding : DynamicResourceExtension
{
    #region Internal Classes

    private class DynamicResourceBindingSource : Freezable
    {
        public static readonly DependencyProperty ResourceReferenceExpressionProperty = DependencyProperty.Register(nameof(ResourceReferenceExpression), 
            typeof(object), typeof(DynamicResourceBindingSource), new FrameworkPropertyMetadata());

        public object ResourceReferenceExpression
        {
            get => GetValue(ResourceReferenceExpressionProperty);
            set => SetValue(ResourceReferenceExpressionProperty, value);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new DynamicResourceBindingSource();
        }
    }

    #endregion Internal Classes

    public DynamicResourceBinding() { }

    public DynamicResourceBinding(string resourceKey) : base(resourceKey)
    { }

    public IValueConverter Converter { get; set; }

    public object ConverterParameter { get; set; }

    public CultureInfo ConverterCulture { get; set; }

    public string StringFormat { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        //Get the expression representing the DynamicResource
        var resourceReferenceExpression = base.ProvideValue(serviceProvider);

        //If there's no converter, nor StringFormat, just return it (Matches standard DynamicResource behavior}
        if (Converter == null && StringFormat == null)
            return resourceReferenceExpression;

        //Create the Freezable-based object and set its ResourceReferenceExpression property directly to the 
        //result of base.ProvideValue (held in resourceReferenceExpression). Then add it to the target FrameworkElement's
        //Resources collection (using itself as its key for uniqueness) so it participates in the resource lookup chain.
        var dynamicResourceBindingSource = new DynamicResourceBindingSource
        {
            ResourceReferenceExpression = resourceReferenceExpression
        };

        //Get the target FrameworkElement so we have access to its Resources collection
        //Note: targetFrameworkElement may be null in the case of setters. Still trying to figure out how to handle them.
        //For now, they just fall back to looking up at the app level
        var targetInfo = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
        var targetFrameworkElement = targetInfo?.TargetObject as FrameworkElement;
        targetFrameworkElement?.Resources.Add(dynamicResourceBindingSource, dynamicResourceBindingSource);

        //Now since we have a source object which has a DependencyProperty that's set to the value of the
        //DynamicResource we're interested in, we simply use that as the source for a new binding, passing in all of the other binding-related properties.
        var binding = new Binding
        {
            Path = new PropertyPath(DynamicResourceBindingSource.ResourceReferenceExpressionProperty),
            Source = dynamicResourceBindingSource,
            Converter = Converter,
            ConverterParameter = ConverterParameter,
            ConverterCulture = ConverterCulture,
            StringFormat = StringFormat,
            Mode = BindingMode.OneWay
        };

        //Now we simply return the result of the new binding's ProvideValue
        //method (or the binding itself if the target is not a FrameworkElement)
        return (targetFrameworkElement != null) ? binding.ProvideValue(serviceProvider) : binding;
    }
}