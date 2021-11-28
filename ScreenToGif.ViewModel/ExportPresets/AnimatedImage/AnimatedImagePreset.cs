namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage;

public class AnimatedImagePreset : ExportPreset
{
    private bool _looped = true;
    private bool _repeatForever = true;
    private int _repeatCount = 2;


    public bool Looped
    {
        get => _looped;
        set => SetProperty(ref _looped, value);
    }

    public bool RepeatForever
    {
        get => _repeatForever;
        set => SetProperty(ref _repeatForever, value);
    }

    public int RepeatCount
    {
        get => _repeatCount;
        set => SetProperty(ref _repeatCount, value);
    }


    public AnimatedImagePreset()
    {
        OutputFilenameKey = "S.Preset.Filename.Animation";
    }
}