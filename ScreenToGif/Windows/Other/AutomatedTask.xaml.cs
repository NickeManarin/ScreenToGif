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
            }
        }

        private void Ok_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLoaded && TypeComboBox.SelectedIndex > 0;
        }

        private void Ok_Executed(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}