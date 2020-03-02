using System;
using System.Globalization;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    public class StageToButtonString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Stage stage))
                return LocalizationHelper.Get("S.Recorder.Record");

            switch (stage)
            {
                case Stage.Stopped:
                    return LocalizationHelper.Get("S.Recorder.Record");
                case Stage.Recording:
                    return LocalizationHelper.Get("S.Recorder.Pause");
                case Stage.Paused:
                    return LocalizationHelper.Get("S.Recorder.Continue");
                case Stage.Snapping:
                    return LocalizationHelper.Get("S.Recorder.Snap");
            }

            return LocalizationHelper.Get("S.Recorder.Record");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}