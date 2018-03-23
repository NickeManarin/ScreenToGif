using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Controls;
using ScreenToGif.Windows;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Util.Model
{
    internal class ApplicationViewModel : ApplicationModel
    {
        #region Commands

        public ICommand OpenLauncher
        {
            get
            {
                return new RelayCommand
                {
                    ExecuteAction = a =>
                    {
                        var startup = Application.Current.Windows.OfType<Startup>().FirstOrDefault();

                        if (startup == null)
                        {
                            startup = new Startup();
                            startup.Closed += (sender, args) => { CloseOrNot(); };

                            startup.Show();
                        }
                        else
                        {
                            if (startup.WindowState == WindowState.Minimized)
                                startup.WindowState = WindowState.Normal;

                            startup.Activate();
                        }
                    }
                };
            }
        }

        public ICommand OpenRecorder
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        //True if all windows are not Recorders.
                        return Application.Current.Windows.OfType<Window>().All(a => !(a is RecorderWindow));
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        caller?.Hide();

                        if (UserSettings.All.NewRecorder)
                        {
                            var recorderNew = new RecorderNew();
                            recorderNew.Closed += (sender, args) =>
                            {
                                var window = sender as RecorderNew;

                                if (window?.Project != null && window.Project.Any)
                                {
                                    ShowEditor(window.Project);
                                    caller?.Close();
                                }
                                else
                                {
                                    caller?.Show();
                                    CloseOrNot();
                                }
                            };

                            Application.Current.MainWindow = recorderNew;
                            recorderNew.Show();

                            return;
                        }

                        var recorder = new Recorder();
                        recorder.Closed += (sender, args) =>
                        {
                            var window = sender as Recorder;

                            if (window?.Project != null && window.Project.Any)
                            {
                                ShowEditor(window.Project);
                                caller?.Close();
                            }
                            else
                            {
                                caller?.Show();
                                CloseOrNot();
                            }
                        };

                        Application.Current.MainWindow = recorder;
                        recorder.Show();
                    }
                };
            }
        }

        public ICommand OpenWebcamRecorder
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        //True if all windows are not Recorders.
                        return Application.Current.Windows.OfType<Window>().All(a => !(a is RecorderWindow));
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        caller?.Hide();

                        var recorder = new Windows.Webcam();
                        recorder.Closed += (sender, args) =>
                        {
                            var window = sender as Windows.Webcam;

                            if (window?.Project != null && window.Project.Any)
                            {
                                ShowEditor(window.Project);
                                caller?.Close();
                            }
                            else
                            {
                                caller?.Show();
                                CloseOrNot();
                            }
                        };

                        Application.Current.MainWindow = recorder;
                        recorder.Show();
                    }
                };
            }
        }

        public ICommand OpenBoardRecorder
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        //True if all windows are not Recorders.
                        return Application.Current.Windows.OfType<Window>().All(a => !(a is RecorderWindow));
                    },
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;
                        caller?.Hide();

                        var recorder = new Board();
                        recorder.Closed += (sender, args) =>
                        {
                            var window = sender as Board;

                            if (window?.Project != null && window.Project.Any)
                            {
                                ShowEditor(window.Project);
                                caller?.Close();
                            }
                            else
                            {
                                caller?.Show();
                                CloseOrNot();
                            }
                        };

                        Application.Current.MainWindow = recorder;
                        recorder.Show();
                    }
                };
            }
        }

        public ICommand OpenEditor
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = a => true, //TODO: Always let this window opens or check if there's any other recorder active?
                    ExecuteAction = a =>
                    {
                        var caller = a as Window;

                        //TODO: Should it behave the same way as it does after a recording? Always open a new one or simply show all/one that was already opened?
                        ShowEditor();

                        caller?.Close();
                    }
                };
            }
        }

        public ICommand OpenOptions
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = a => true, //TODO: Always let this window opens or check if there's any other recorder active?
                    ExecuteAction = a =>
                    {
                        var options = Application.Current.Windows.OfType<Options>().FirstOrDefault();

                        if (options == null)
                        {
                            options = new Options();
                            options.Closed += (sender, args) =>
                            {
                                CloseOrNot();
                            };

                            //TODO: Open as dialog or not? Block other windows?
                            options.Show();
                        }
                        else
                        {
                            if (options.WindowState == WindowState.Minimized)
                                options.WindowState = WindowState.Normal;

                            options.Activate();
                        }
                    }
                };
            }
        }

        public ICommand ExitApplication
        {
            get
            {
                return new RelayCommand
                {
                    CanExecutePredicate = o =>
                    {
                        return Application.Current.Windows.OfType<RecorderWindow>().All(a => a.Stage != Stage.Recording);
                    },
                    ExecuteAction = a =>
                    {
                        //TODO: Check if there's anything open or anything happening with editors.

                        if (!Dialog.Ask(LocalizationHelper.Get("Application.Exiting.Title"), LocalizationHelper.Get("Application.Exiting.Instruction"), LocalizationHelper.Get("Application.Exiting.Message")))
                            return;

                        Application.Current.Shutdown(69);
                    }
                };
            }
        }

        #endregion

        private void ShowEditor(ProjectInfo project = null)
        {
            var editor = Application.Current.Windows.OfType<Editor>().FirstOrDefault(f => f.Project == null || !f.Project.Any);

            if (editor == null)
            {
                editor = new Editor { Project = project };
                editor.Closed += (sender, args) => CloseOrNot();
                editor.Show();
            }
            else
            {
                //TODO: Three modes for opening the editor:
                //Always open a new window.
                //Open a new window if there's no window without any project loaded.
                //Open a new window if there's no idle window (with a project loaded).

                //TODO: Detect if the last state was normal/maximized.
                if (editor.WindowState == WindowState.Minimized)
                    editor.WindowState = WindowState.Normal;

                if (project != null)
                    editor.LoadProject(project, true, false);

                editor.Activate();
            }

            Application.Current.MainWindow = editor;
        }

        private void CloseOrNot()
        {
            //When closed, check if it's the last window, then close if it's the configured behavior.
            if (!UserSettings.All.ShowNotificationIcon || !UserSettings.All.KeepOpen)
            {
                if (Application.Current.Windows.Count == 0)
                    Application.Current.Shutdown(2);
            }
        }
    }

    internal class RelayCommand : ICommand
    {
        public Predicate<object> CanExecutePredicate { get; set; }
        public Action<object> ExecuteAction { get; set; }

        public RelayCommand(Predicate<object> canExecute, Action<object> execute)
        {
            CanExecutePredicate = canExecute;
            ExecuteAction = execute;
        }

        public RelayCommand()
        { }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return CanExecutePredicate == null || CanExecutePredicate(parameter);
        }

        public void Execute(object parameter)
        {
            ExecuteAction(parameter);
        }
    }

    internal class AdvancedRelayCommand : RoutedUICommand, ICommand
    {
        public Predicate<object> CanExecutePredicate { get; set; }
        public Action<object> ExecuteAction { get; set; }

        public AdvancedRelayCommand()
        { }

        public AdvancedRelayCommand(string text, string name, Type ownerType, InputGestureCollection inputGestures) : base(text, name, ownerType, inputGestures)
        { }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecutePredicate == null || CanExecutePredicate(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAction(parameter);
        }

        //public bool CanExecute(object parameter)
        //{
        //    return CanExecutePredicate == null || CanExecutePredicate(parameter);
        //}

        //public void Execute(object parameter)
        //{
        //    ExecuteAction(parameter);
        //}
    }
}