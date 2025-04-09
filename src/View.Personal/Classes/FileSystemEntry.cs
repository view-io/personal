namespace View.Personal.Classes
{
    using Material.Icons;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class FileSystemEntry : INotifyPropertyChanged
    {
        private bool _isWatched;
        private bool _isCheckBoxEnabled = true; // Default to enabled

        public string Name { get; set; }
        public string Size { get; set; }
        public string LastModified { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }

        public bool IsWatched
        {
            get => _isWatched;
            set
            {
                if (_isWatched != value)
                {
                    _isWatched = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCheckBoxEnabled
        {
            get => _isCheckBoxEnabled;
            set
            {
                if (_isCheckBoxEnabled != value)
                {
                    _isCheckBoxEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public MaterialIconKind IconKind =>
            IsDirectory ? MaterialIconKind.Folder : MaterialIconKind.FileOutline;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}