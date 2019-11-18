using System.ComponentModel;
using System.Runtime.Serialization;
using ScreenToGif.Util;

namespace ScreenToGif.Model
{
    public class DefaultTaskModel : BindableBase
    {
        #region Variables

        public enum TaskTypeEnum
        {
            NotDeclared = 0,
            MouseClicks = 1,
            KeyStrokes = 2,
            Delay = 3,
            Progress = 4,
            Border = 5,
            Shadow = 6,
            RemoveDuplicates = 7,
            Watermark = 8,
            TitleFrame = 9,
            Resize = 10,
        }

        private TaskTypeEnum _taskType = TaskTypeEnum.NotDeclared;
        private string _image = null;

        #endregion

        public TaskTypeEnum TaskType
        {
            get => _taskType;
            set => SetProperty(ref _taskType, value);
        }

        public string Kind
        {
            get
            {
                switch (TaskType)
                {
                    case TaskTypeEnum.MouseClicks:
                        return LocalizationHelper.Get("Editor.Image.Clicks", true);
                    case TaskTypeEnum.KeyStrokes:
                        return LocalizationHelper.Get("Editor.Image.KeyStrokes", true);
                    case TaskTypeEnum.Delay:
                        return LocalizationHelper.Get("S.DelayUpdate", true);
                    case TaskTypeEnum.Progress:
                        return LocalizationHelper.Get("Editor.Image.Progress", true);
                    case TaskTypeEnum.Border:
                        return LocalizationHelper.Get("Editor.Image.Border", true);
                    case TaskTypeEnum.Shadow:
                        return LocalizationHelper.Get("Editor.Image.Shadow", true);
                    default:
                        return LocalizationHelper.Get("S.Options.Tasks.SelectType");
                }
            }
        }

        public string Details => ToString();

        [IgnoreDataMember] //This attribute is getting ignored.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }


        public DefaultTaskModel ShallowCopy()
        {
            return (DefaultTaskModel)MemberwiseClone();
        }
    }
}