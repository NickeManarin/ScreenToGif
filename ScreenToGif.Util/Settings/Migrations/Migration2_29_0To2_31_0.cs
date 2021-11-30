using ScreenToGif.Domain.Models;

namespace ScreenToGif.Settings.Migrations;

internal class Migration2_29_0To2_31_0
{
    internal static bool Up(List<Property> properties)
    {
        //Rename properties.
        var mouseClickColor = properties.FirstOrDefault(f => f.Key == "MouseClicksColor");

        if (mouseClickColor != null)
            mouseClickColor.Key = "LeftMouseButtonClicksColor";

        var tasks = properties.FirstOrDefault(f => f.Key == "AutomatedTasksList");

        if (tasks != null)
        {
            foreach (var child in tasks.Children)
            {
                if (child.Type == "MouseClicksModel")
                {
                    foreach (var attribute in child.Attributes)
                    {
                        if (attribute.Key == "ForegroundColor")
                        {
                            attribute.Key = "LeftButtonForegroundColor";
                            break;
                        }
                    }
                }
            }
        }

        return true;
    }
}