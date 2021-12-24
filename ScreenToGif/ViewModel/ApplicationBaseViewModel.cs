using ScreenToGif.Domain.ViewModels;

namespace ScreenToGif.ViewModel;

internal class ApplicationBaseViewModel : BaseViewModel
{
    #region Variables

    private string _recorderGesture;
    private string _webcamRecorderGesture;
    private string _boardRecorderGesture;
    private string _editorGesture;
    private string _optionsGesture;
    private string _exitGesture;
    
    #endregion

    #region Properties

    public string RecorderGesture
    {
        get => _recorderGesture;
        set => SetProperty(ref _recorderGesture, value);
    }

    public string WebcamRecorderGesture
    {
        get => _webcamRecorderGesture;
        set => SetProperty(ref _webcamRecorderGesture, value);
    }

    public string BoardRecorderGesture
    {
        get => _boardRecorderGesture;
        set => SetProperty(ref _boardRecorderGesture, value);
    }

    public string EditorGesture
    {
        get => _editorGesture;
        set => SetProperty(ref _editorGesture, value);
    }

    public string OptionsGesture
    {
        get => _optionsGesture;
        set => SetProperty(ref _optionsGesture, value);
    }

    public string ExitGesture
    {
        get => _exitGesture;
        set => SetProperty(ref _exitGesture, value);
    }

    #endregion
}