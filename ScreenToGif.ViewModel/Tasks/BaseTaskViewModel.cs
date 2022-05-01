using ScreenToGif.Util;
using System.ComponentModel;
using System.Runtime.Serialization;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.ViewModels;

namespace ScreenToGif.ViewModel.Tasks;

public class BaseTaskViewModel : BindableBase, IPersistent
{
    #region Variables

    private TaskTypes _taskType = TaskTypes.NotDeclared;
    private bool _isEnabled = true;
    private bool _isManual = false;
    private string _image = null;

    #endregion

    public TaskTypes TaskType
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
                case TaskTypes.MouseEvents:
                    return LocalizationHelper.Get("S.Editor.Image.MouseEvents", true);
                case TaskTypes.KeyStrokes:
                    return LocalizationHelper.Get("S.Editor.Image.KeyStrokes", true);
                case TaskTypes.Delay:
                    return LocalizationHelper.Get("S.Delay.Update", true);
                case TaskTypes.Progress:
                    return LocalizationHelper.Get("S.Editor.Image.Progress", true);
                case TaskTypes.Border:
                    return LocalizationHelper.Get("S.Editor.Image.Border", true);
                case TaskTypes.Shadow:
                    return LocalizationHelper.Get("S.Editor.Image.Shadow", true);
                case TaskTypes.Resize:
                    return LocalizationHelper.Get("S.Editor.Image.Resize", true);
                default:
                    return LocalizationHelper.Get("S.Options.Tasks.SelectType");
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string Details => ToString();

    [DataMember(EmitDefaultValue = false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsManual
    {
        get => _isManual;
        set => SetProperty(ref _isManual, value);
    }

    [DataMember(EmitDefaultValue = false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }


    public BaseTaskViewModel ShallowCopy()
    {
        return (BaseTaskViewModel) MemberwiseClone();
    }

    public virtual void Persist()
    { }
}