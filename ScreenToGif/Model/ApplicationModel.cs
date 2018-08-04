using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScreenToGif.Model
{
    internal class ApplicationModel : INotifyPropertyChanged
    {
        #region Variables

        private string _recorderGesture;
        private string _webcamRecorderGesture;
        private string _boardRecorderGesture;
        private string _editorGesture;
        private string _optionsGesture;
        private string _exitGesture;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        public string RecorderGesture
        {
            get => _recorderGesture;
            set
            {
                _recorderGesture = value;
                OnPropertyChanged();
            }
        }

        public string WebcamRecorderGesture
        {
            get => _webcamRecorderGesture;
            set
            {
                _webcamRecorderGesture = value;
                OnPropertyChanged();
            }
        }

        public string BoardRecorderGesture
        {
            get => _boardRecorderGesture;
            set
            {
                _boardRecorderGesture = value;
                OnPropertyChanged();
            }
        }

        public string EditorGesture
        {
            get => _editorGesture;
            set
            {
                _editorGesture = value;
                OnPropertyChanged();
            }
        }

        public string OptionsGesture
        {
            get => _optionsGesture;
            set
            {
                _optionsGesture = value;
                OnPropertyChanged();
            }
        }

        public string ExitGesture
        {
            get => _exitGesture;
            set
            {
                _exitGesture = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}