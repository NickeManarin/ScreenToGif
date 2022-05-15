using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Domain.Models.Project;
using ScreenToGif.Domain.ViewModels;

namespace ScreenToGif.ViewModel;

public class EditorViewModel : BaseViewModel
{
    #region Variables

    private Project _project = null;
    private TimeSpan _currentTime = TimeSpan.Zero;
    private int _currentIndex = -1;
    private WriteableBitmap _renderedImage = null;
    private double _zoom = 1d;

    private ObservableCollection<FrameViewModel> _frames = new();

    #endregion

    #region Properties

    public CommandBindingCollection CommandBindings => new()
    {
        new CommandBinding(FindCommand("Command.NewRecording"), (sender, args) => { Console.WriteLine(""); }, (sender, args) => { args.CanExecute = true; }),
        new CommandBinding(FindCommand("Command.NewWebcamRecording"), (sender, args) => { Console.WriteLine(""); }, (sender, args) => { args.CanExecute = true; }),
    };

    public Project Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    public TimeSpan CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        set => SetProperty(ref _currentIndex, value);
    }

    internal WriteableBitmap RenderedImage
    {
        get => _renderedImage;
        set => SetProperty(ref _renderedImage, value);
    }

    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, value);
    }


    //In use for Version < 3.0


    /// <summary>
    /// The list of frames.
    /// </summary>
    public ObservableCollection<FrameViewModel> Frames
    {
        get => _frames;
        set => SetProperty(ref _frames, value);
    }

    #endregion

    public EditorViewModel()
    {

    }

    #region Methods

    internal void Init()
    {
        RenderedImage = new WriteableBitmap(Project.Width, Project.Height, Project.HorizontalDpi, Project.VerticalDpi, PixelFormats.Bgra32, null);
    }

    internal void Render()
    {
        //Display mode: By timestamp or frame index.
        //Display properties in Statistic tab.

        //Get current timestamp/index and render the scene and apply to the RenderedImage property.

        //How are previews going to work?
        //  Text rendering
        //  Rendering that needs access to the all layers.
        //  Rendering that changes the size of the canvas.

        //Preview quality.
        //Render the list preview for the frames.
    }

    //How are the frames/data going to be stored in the disk?
    //Project file for the user + opened project should have a cache
    //  Project file for user: I'll need to create a file spec.
    //  Cache folder for the app:

    //As a single cache for each track? (storing as pixel array, to improve performance)
    //I'll need a companion json with positions and other details.
    //I also need to store in memory for faster usage.

    #endregion
}