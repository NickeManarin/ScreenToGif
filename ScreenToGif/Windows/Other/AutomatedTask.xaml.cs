using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScreenToGif.Domain.Enums;
using ScreenToGif.UserControls;
using ScreenToGif.Util;
using ScreenToGif.ViewModel.Tasks;

namespace ScreenToGif.Windows.Other;

public partial class AutomatedTask : Window
{
    public BaseTaskViewModel CurrentTask { get; set; }

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
            MainBorder.Background = TryFindResource("Vector.Pen") as Brush;
            TypeTextBlock.Text = LocalizationHelper.Get("S.Edit");
            TypeComboBox.SelectedIndex = (int)(CurrentTask?.TaskType ?? TaskTypes.NotDeclared);
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
            switch ((TaskTypes)TypeComboBox.SelectedIndex)
            {
                case TaskTypes.MouseEvents:
                    CurrentTask = MouseEventsViewModel.Default();
                    break;
                case TaskTypes.KeyStrokes:
                    CurrentTask = KeyStrokesViewModel.Default();
                    break;
                case TaskTypes.Delay:
                    CurrentTask = DelayViewModel.Default();
                    break;
                case TaskTypes.Progress:
                    CurrentTask = ProgressViewModel.Default();
                    break;
                case TaskTypes.Border:
                    CurrentTask = BorderViewModel.Default();
                    break;
                case TaskTypes.Shadow:
                    CurrentTask = ShadowViewModel.Default();
                    break;
            }
        }

        switch ((TaskTypes)TypeComboBox.SelectedIndex)
        {
            case TaskTypes.MouseEvents:
                MainPresenter.Content = new MouseEventsPanel { DataContext = CurrentTask };
                break;
            case TaskTypes.KeyStrokes:
                MainPresenter.Content = new KeyStrokesPanel { DataContext = CurrentTask };
                break;
            case TaskTypes.Delay:
                MainPresenter.Content = new DelayPanel { DataContext = CurrentTask };
                break;
            case TaskTypes.Progress:
                MainPresenter.Content = new ProgressPanel { DataContext = CurrentTask };
                break;
            case TaskTypes.Border:
                MainPresenter.Content = new BorderPanel { DataContext = CurrentTask };
                break;
            case TaskTypes.Shadow:
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