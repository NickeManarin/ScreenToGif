using ScreenToGif.Domain.Models;

namespace ScreenToGif.ViewModel;

public class WebcamViewModel : RecorderViewModel
{
    private List<VideoSource> _videoSources = new();
    private VideoSource _selectedVideoSource = null;
    private MediaSource _selectedMediaSource = null;
        
    public List<VideoSource> VideoSources
    {
        get => _videoSources;
        set => SetProperty(ref _videoSources, value);
    }      
        
    public VideoSource SelectedVideoSource
    {
        get => _selectedVideoSource;
        set => SetProperty(ref _selectedVideoSource, value);
    }

    public MediaSource SelectedMediaSource
    {
        get => _selectedMediaSource;
        set => SetProperty(ref _selectedMediaSource, value);
    }
}