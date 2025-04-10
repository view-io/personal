namespace View.Personal.Classes
{
    using Material.Icons;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class FileSystemEntry : INotifyPropertyChanged
    {
        private bool _isWatched;
        private bool _isWatchedOrInherited;
        private bool _isCheckBoxEnabled = true;
        private bool _containsWatchedItems;
        private bool _isSelectedWatchedDirectory; // New property

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

        public bool IsWatchedOrInherited
        {
            get => _isWatchedOrInherited;
            set
            {
                if (_isWatchedOrInherited != value)
                {
                    _isWatchedOrInherited = value;
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

        public bool ContainsWatchedItems
        {
            get => _containsWatchedItems;
            set
            {
                if (_containsWatchedItems != value)
                {
                    _containsWatchedItems = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelectedWatchedDirectory
        {
            get => _isSelectedWatchedDirectory;
            set
            {
                if (_isSelectedWatchedDirectory != value)
                {
                    _isSelectedWatchedDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

        public MaterialIconKind IconKind =>
            IsDirectory ? MaterialIconKind.FolderOutline : MaterialIconKind.FileOutline;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}