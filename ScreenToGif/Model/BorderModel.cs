﻿using System.Windows.Media;
using ScreenToGif.Util;

namespace ScreenToGif.Model
{
    public class BorderModel : DefaultTaskModel
    {
        #region Variables

        private Color _color;
        private double _leftThickness;
        private double _topThickness;
        private double _rightThickness;
        private double _bottomThickness;
        
        #endregion

        public BorderModel()
        {
            TaskType = TaskTypeEnum.Border;
        }

        public Color Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public double LeftThickness
        {
            get => _leftThickness;
            set => SetProperty(ref _leftThickness, value);
        }

        public double TopThickness
        {
            get => _topThickness;
            set => SetProperty(ref _topThickness, value);
        }

        public double RightThickness
        {
            get => _rightThickness;
            set => SetProperty(ref _rightThickness, value);
        }

        public double BottomThickness
        {
            get => _bottomThickness;
            set => SetProperty(ref _bottomThickness, value);
        }

        public override string ToString()
        {
            return $"{LocalizationHelper.Get("Color")} #{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}, " +
                   $"{(LocalizationHelper.Get("Caption.Thickness"))} ({LeftThickness}, {TopThickness}, {LeftThickness}, {BottomThickness})";
        }

        public static BorderModel Default()
        {
            return new BorderModel
            {
                Color = Color.FromArgb(255, 0, 0, 0),
                LeftThickness = 1,
                TopThickness = 1,
                RightThickness = 1,
                BottomThickness = 1,
            };
        }

        public static BorderModel FromSettings()
        {
            return new BorderModel
            {
                Color = UserSettings.All.BorderColor,
                LeftThickness = UserSettings.All.BorderLeftThickness,
                TopThickness = UserSettings.All.BorderTopThickness,
                RightThickness = UserSettings.All.BorderRightThickness,
                BottomThickness = UserSettings.All.BorderBottomThickness,
            };
        }
    }
}