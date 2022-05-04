using System.Globalization;
using System.Windows.Data;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Util.Converters;

public class StageToButtonString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RecorderStages stage)
            return LocalizationHelper.Get("S.Recorder.Record");

        switch (stage)
        {
            case RecorderStages.Stopped:
                return LocalizationHelper.Get("S.Recorder.Record");
            case RecorderStages.Recording:
                return LocalizationHelper.Get("S.Recorder.Pause");
            case RecorderStages.Paused:
                return LocalizationHelper.Get("S.Recorder.Continue");
            case RecorderStages.Snapping:
                return LocalizationHelper.Get("S.Recorder.Snap");
        }

        return LocalizationHelper.Get("S.Recorder.Record");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}