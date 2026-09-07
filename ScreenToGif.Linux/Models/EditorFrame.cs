using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ScreenToGif.Linux.Models;

public sealed class EditorFrame : INotifyPropertyChanged, IDisposable
{
    private int _delayMs;
    private Bitmap? _thumbnail;

    public EditorFrame(string filePath, int delayMs)
    {
        FilePath = filePath;
        _delayMs = Math.Max(1, delayMs);
    }

    public string FilePath { get; set; }

    public int DelayMs
    {
        get => _delayMs;
        set
        {
            var delay = Math.Max(1, value);

            if (_delayMs == delay)
                return;

            _delayMs = delay;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DelayText));
        }
    }

    [JsonIgnore]
    public string DisplayName => Path.GetFileName(FilePath);

    private int _frameNumber;

    [JsonIgnore]
    public int FrameNumber
    {
        get => _frameNumber;
        set
        {
            if (_frameNumber == value)
                return;

            _frameNumber = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public string DelayText => $"{DelayMs} ms";

    [JsonIgnore]
    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail?.Dispose();
            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        _thumbnail?.Dispose();
        _thumbnail = null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
