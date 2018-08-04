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
            ProgressBar = 3,
            RemoveDuplicates = 4,
            Watermark = 5
        }

        private TaskTypeEnum _taskType = TaskTypeEnum.NotDeclared;

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
                }

                return "";
            }
        }

        public string Details => ToString();


        public DefaultTaskModel ShallowCopy()
        {
            return (DefaultTaskModel)MemberwiseClone();
        }
    }
}