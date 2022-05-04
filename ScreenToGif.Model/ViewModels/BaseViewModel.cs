using System.Windows;
using System.Windows.Input;

namespace ScreenToGif.Domain.ViewModels
{
    public class BaseViewModel : BindableBase
    {
        #region Commands

        //Instead of getting the command from the Commands.xaml, I can create them here.
        //private RoutedUICommand _newRecordingCommand = new RoutedUICommand
        //{
        //    Text = "S.Command.NewRecording",
        //    InputGestures = { new KeyGesture(Key.N, ModifierKeys.Control) }
        //};

        //public RoutedUICommand NewRecordingCommand
        //{
        //    get => _newRecordingCommand;
        //    set => SetProperty(ref _newRecordingCommand, value);
        //}



        //I can also create it statically.
        //public static RoutedUICommand NewRecordingCommand { get; set; } = new RoutedUICommand
        //{
        //    Text = "S.Command.NewRecording",
        //    InputGestures = { new KeyGesture(Key.N, ModifierKeys.Control) }
        //};

        //public CommandBindingCollection CommandBindings => new CommandBindingCollection
        //{
        //    new CommandBinding(NewRecordingCommand, (sender, args) => { Console.WriteLine(""); }, (sender, args) => { args.CanExecute = true; })
        //};

        #endregion

        #region Helper methods

        protected internal RoutedUICommand FindCommand(string key)
        {
            return Application.Current.FindResource(key) as RoutedUICommand;
        }
        
        #endregion
    }
}