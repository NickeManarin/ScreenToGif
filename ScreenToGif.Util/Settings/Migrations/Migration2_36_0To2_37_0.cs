using ScreenToGif.Domain.Models;

namespace ScreenToGif.Settings.Migrations;

internal class Migration2_36_0To2_37_0
{
    internal static bool Up(List<Property> properties)
    {
        // Rename properties
        var mouseClicksWidth = properties.FirstOrDefault(a => a.Key == "MouseClicksWidth");
        if (mouseClicksWidth != null)
        {
            mouseClicksWidth.Key = "MouseEventsWidth";
        }

        var mouseClicksHeight = properties.FirstOrDefault(a => a.Key == "MouseClicksHeight");
        if (mouseClicksHeight != null)
        {
            mouseClicksHeight.Key = "MouseEventsHeight";
        }

        // Add new property
        if (!properties.Any(a => a.Key == "MouseHighlightColor"))
        {
            properties.Add(new Property()
            {
                Key = "MouseHighlightColor",
                Type = "Color",
                Value = "#000000FF",
            });
        }

        UpdateTasks(properties);

        return true;
    }

    private static void UpdateTasks(List<Property> properties)
    {
        // Update tasks
        var tasks = properties.FirstOrDefault(f => f.Key == "AutomatedTasksList");

        if (tasks != null)
        {
            foreach (var task in tasks.Children)
            {
                if (task.Type == "MouseClicksViewModel")
                {
                    task.Type = "MouseEventsViewModel";
                    var taskTypeAttribute = task.Attributes.FirstOrDefault(a => a.Key == "TaskType");
                    if (taskTypeAttribute != null)
                    {
                        taskTypeAttribute.Value = "MouseEvents";
                    }

                    if (!task.Attributes.Any(a => a.Key == "HighlightForegroundColor"))
                    {
                        task.Attributes.Add(new Property()
                        {
                            Key = "HighlightForegroundColor",
                            Value = "#000000FF",
                        });
                    }
                }
            }
        }
    }
}
