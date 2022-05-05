using System.Windows.Media;

namespace ScreenToGif.Domain.Models.Project;

public class Project
{
    #region Identity

    /// <summary>
    /// Just the name to the project file.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The full path of the project (saved by the user).
    /// It's the path + filename + extension.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// The version of ScreenToGif used to create this project.
    /// </summary>
    public Version Version { get; set; }

    public DateTime? CreationDate { get; set; }

    public DateTime? LastModificationDate { get; set; }

    #endregion

    #region Visual

    /// <summary>
    /// The canvas width of the project.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// The canvas height of the project.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// The DPI of the X axis of the project.
    /// </summary>
    public double HorizontalDpi { get; set; }

    /// <summary>
    /// The DPI of the Y axis of the project.
    /// </summary>
    public double VerticalDpi { get; set; }

    /// <summary>
    /// The background of the whole project.
    /// </summary>
    public Brush Background { get; set; }

    #endregion

    /// <summary>
    /// Tracks can hold multiple sequences of the same type, but not overlapping.
    /// </summary>
    public List<Track> Tracks { get; set; }
}