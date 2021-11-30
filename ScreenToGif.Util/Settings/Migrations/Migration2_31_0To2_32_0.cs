using ScreenToGif.Domain.Models;

namespace ScreenToGif.Settings.Migrations;

internal class Migration2_31_0To2_32_0
{
    internal static bool Up(List<Property> properties)
    {
        //Rename properties.
        var tasks = properties.FirstOrDefault(f => f.Key == "AutomatedTasksList");

        if (tasks != null)
        {
            foreach (var child in tasks.Children)
            {
                child.NameSpace = "clr-namespace:ScreenToGif.ViewModel.Tasks;assembly=ScreenToGif";

                switch (child.Type)
                {
                    case "MouseClicksModel":
                    {
                        child.Type = "MouseClicksViewModel";
                        break;
                    }
                    case "KeyStrokesModel":
                    {
                        child.Type = "KeyStrokesViewModel";
                        break;
                    }
                    case "DelayModel":
                    {
                        child.Type = "DelayViewModel";
                        break;
                    }
                    case "ProgressModel":
                    {
                        child.Type = "ProgressViewModel";
                        break;
                    }
                    case "BorderModel":
                    {
                        child.Type = "BorderViewModel";
                        break;
                    }
                    case "ShadowModel":
                    {
                        child.Type = "ShadowViewModel";
                        break;
                    }
                }
            }
        }

        return true;
    }
}