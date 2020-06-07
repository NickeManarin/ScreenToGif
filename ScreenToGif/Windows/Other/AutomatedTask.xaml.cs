using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Model;
using ScreenToGif.UserControls;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class AutomatedTask : Window
    {
        public DefaultTaskModel CurrentTask { get; set; }

        public bool IsEditing { get; set; }

        public AutomatedTask()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TypeComboBox.Focus();

            if (IsEditing)
            {
                MainViewbox.Child = TryFindResource("Vector.Pen") as Canvas;
                TypeTextBlock.Text = LocalizationHelper.Get("S.Edit");
                TypeComboBox.SelectedIndex = (int)(CurrentTask?.TaskType ?? DefaultTaskModel.TaskTypeEnum.NotDeclared);
                TypeComboBox.IsEnabled = false;
                EnabledCheckBox.Visibility = Visibility.Visible;

                TypeComboBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void TypeComboBox_Selected(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (TypeComboBox.SelectedIndex < 1)
            {
                MainPresenter.Content = null;
                return;
            }

            if (!IsEditing)
            {
                //Create a new model.
                switch ((DefaultTaskModel.TaskTypeEnum)TypeComboBox.SelectedIndex)
                {
                    case DefaultTaskModel.TaskTypeEnum.MouseClicks:
                        CurrentTask = MouseClicksModel.Default();
                        break;
                    case DefaultTaskModel.TaskTypeEnum.KeyStrokes:
                        CurrentTask = KeyStrokesModel.Default();
                        break;
                    case DefaultTaskModel.TaskTypeEnum.Delay:
                        CurrentTask = DelayModel.Default();
                        break;
                    case DefaultTaskModel.TaskTypeEnum.Progress:
                        CurrentTask = ProgressModel.Default();
                        break;
                    case DefaultTaskModel.TaskTypeEnum.Border:
                        CurrentTask = BorderModel.Default();
                        break;
                    case DefaultTaskModel.TaskTypeEnum.Shadow:
                        CurrentTask = ShadowModel.Default();
                        break;
                }
            }

            switch ((DefaultTaskModel.TaskTypeEnum)TypeComboBox.SelectedIndex)
            {
                case DefaultTaskModel.TaskTypeEnum.MouseClicks:
                    MainPresenter.Content = new MouseClicksPanel { DataContext = CurrentTask };
                    break;
                case DefaultTaskModel.TaskTypeEnum.KeyStrokes:
                    MainPresenter.Content = new KeyStrokesPanel { DataContext = CurrentTask };
                    break;
                case DefaultTaskModel.TaskTypeEnum.Delay:
                    MainPresenter.Content = new DelayPanel { DataContext = CurrentTask };
                    break;
                case DefaultTaskModel.TaskTypeEnum.Progress:
                    MainPresenter.Content = new ProgressPanel { DataContext = CurrentTask };
                    break;
                case DefaultTaskModel.TaskTypeEnum.Border:
                    MainPresenter.Content = new BorderPanel { DataContext = CurrentTask };
                    break;
                case DefaultTaskModel.TaskTypeEnum.Shadow:
                    MainPresenter.Content = new ShadowPanel { DataContext = CurrentTask };
                    break;
            }
        }

        private void Ok_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLoaded && TypeComboBox.SelectedIndex > 0;
        }

        private void Ok_Executed(object sender, RoutedEventArgs e)
        {
            OkButton.Focus();

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}