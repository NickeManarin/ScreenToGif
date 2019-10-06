using System.Collections;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ScreenToGif.Controls.Ribbon
{
    public class Ribbon : Selector
    {
        #region Properties

        public static readonly DependencyProperty TabsProperty = DependencyProperty.Register(nameof(Tabs), typeof(IEnumerable), typeof(Ribbon), new FrameworkPropertyMetadata(null));

        public IEnumerable Tabs
        {
            get => (IEnumerable)GetValue(TabsProperty);
            set => SetValue(TabsProperty, value);
        }
        
        #endregion

        static Ribbon()
        {

        }

        public Ribbon()
        {
            
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();


        }

        //Groups of elements, ordered
        //Groups can be hidden by type/tags
        //
    }
}