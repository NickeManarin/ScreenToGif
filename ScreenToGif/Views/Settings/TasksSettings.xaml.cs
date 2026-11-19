using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel.Tasks;
using ScreenToGif.Windows.Other;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Views.Settings;

public partial class TasksSettings : Page
{
    /// <summary>
    /// List of tasks.
    /// </summary>
    private ObservableCollection<BaseTaskViewModel> _effectList;

    public TasksSettings()
    {
        InitializeComponent();
    }

    private void TasksSettings_Loaded(object sender, RoutedEventArgs e)
    {
        var list = UserSettings.All.AutomatedTasksList?.Cast<BaseTaskViewModel>().ToList() ?? [];

        TasksDataGrid.ItemsSource = _effectList = new ObservableCollection<BaseTaskViewModel>(list);

        foreach (var task in _effectList)
            Watch(task);
    }

    private void TasksSettings_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_effectList == null)
            return;

        foreach (var task in _effectList)
            Unwatch(task);
    }

    /// <summary>
    /// A tick changes only the task, so the list is set again to be saved (#1487).
    /// </summary>
    private void Watch(BaseTaskViewModel task)
    {
        task.PropertyChanged -= Task_PropertyChanged;
        task.PropertyChanged += Task_PropertyChanged;
    }

    private void Unwatch(BaseTaskViewModel task)
    {
        task.PropertyChanged -= Task_PropertyChanged;
    }

    private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        //A copy being edited in the task dialog carries this handler too; only a task in the list counts.
        if (e.PropertyName != nameof(BaseTaskViewModel.IsEnabled) || sender is not BaseTaskViewModel task || !_effectList.Contains(task))
            return;

        UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
    }

    private void MoveUp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TasksPanel.IsVisible && TasksDataGrid.SelectedIndex > 0;
    }

    private void MoveDown_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TasksPanel.IsVisible && TasksDataGrid.SelectedIndex > -1 && TasksDataGrid.SelectedIndex < TasksDataGrid.Items.Count - 1;
    }

    private void Remove_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TasksPanel.IsVisible && TasksDataGrid.SelectedIndex != -1;
    }

    private void Add_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = TasksPanel.IsVisible;
    }

    private void MoveUp_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var selectedIndex = TasksDataGrid.SelectedIndex;
        var selected = _effectList[selectedIndex];

        _effectList.RemoveAt(selectedIndex);
        _effectList.Insert(selectedIndex - 1, selected);
        TasksDataGrid.SelectedItem = selected;

        UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
    }

    private void MoveDown_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var selectedIndex = TasksDataGrid.SelectedIndex;
        var selected = _effectList[selectedIndex];

        _effectList.RemoveAt(selectedIndex);
        _effectList.Insert(selectedIndex + 1, selected);
        TasksDataGrid.SelectedItem = selected;

        UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
    }

    private void Add_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var eff = new AutomatedTask();
        var result = eff.ShowDialog();

        if (result != true)
            return;

        _effectList.Add(eff.CurrentTask);
        Watch(eff.CurrentTask);
        UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
    }

    private void Edit_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var index = TasksDataGrid.SelectedIndex;
        var selected = _effectList[index].ShallowCopy();

        var eff = new AutomatedTask { CurrentTask = selected, IsEditing = true };
        var result = eff.ShowDialog();

        if (result != true)
            return;

        Unwatch(_effectList[index]);
        _effectList[index] = eff.CurrentTask;
        Watch(eff.CurrentTask);
        TasksDataGrid.Items.Refresh();
        TasksDataGrid.SelectedIndex = index;

        UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
    }

    private void Remove_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var index = TasksDataGrid.SelectedIndex;
        Unwatch(_effectList[index]);
        _effectList.RemoveAt(TasksDataGrid.SelectedIndex);

        //Automatically selects the closest item from the position of the one that was removed.
        TasksDataGrid.SelectedIndex = _effectList.Count == 0 ? -1 : _effectList.Count <= index ? _effectList.Count - 1 : index;

        UserSettings.All.AutomatedTasksList = new ArrayList(_effectList.ToArray());
    }

    private void TasksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EditCommandBinding.Command.CanExecute(sender))
            EditCommandBinding.Command.Execute(sender);
    }

    private void TasksDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && EditCommandBinding.Command.CanExecute(sender))
        {
            EditCommandBinding.Command.Execute(sender);
            e.Handled = true;
        }

        if (e.Key == Key.Space)
        {
            if (TasksDataGrid.SelectedItem is not BaseTaskViewModel selected)
                return;

            selected.IsEnabled = !selected.IsEnabled;
            e.Handled = true;
        }
    }
}