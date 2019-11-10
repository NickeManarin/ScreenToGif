using System.Collections.Generic;
using System.Windows.Markup;

namespace ScreenToGif.Controls.Ribbon
{
    [ContentProperty("SizeDefinitions")]
    public class GroupSizeDefinition
    {
        public List<ItemSizeDefinition> SizeDefinitions { get; set; } = new List<ItemSizeDefinition>();

        public bool IsCollapsed { get; set; }
    }
}