namespace View.Personal.Classes
{
    using Material.Icons;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a file system entry in the Data Monitor UI, with properties for display and watch status.
    /// </summary>
    public class FileSystemEntry : INotifyPropertyChanged
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        // ReSharper disable UnusedAutoPropertyAccessor.Global

        #region Public-Members

        /// <summary>
        /// Gets or sets the name of the file system entry.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the size of the file system entry, formatted as a string.
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// Gets or sets the last modified date of the file system entry, formatted as a string.
        /// </summary>
        public string LastModified { get; set; }

        /// <summary>
        /// Gets or sets the full path of the file system entry.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets whether the file system entry is a directory.
        /// </summary>
        public bool IsDirectory { get; set; }

        #endregion

        #region Private-Members

        private bool _isWatched;
        private bool _isWatchedOrInherited;
        private bool _isCheckBoxEnabled = true;
        private bool _containsWatchedItems;
        private bool _isSelectedWatchedDirectory;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Gets or sets whether the file system entry is explicitly watched.
        /// </summary>
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

        /// <summary>
        /// Gets or sets whether the file system entry is watched explicitly or inherited from a parent directory.
        /// </summary>
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

        /// <summary>
        /// Gets or sets whether the watch checkbox is enabled for the file system entry.
        /// </summary>
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

        /// <summary>
        /// Gets or sets whether the file system entry contains watched items.
        /// </summary>
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

        /// <summary>
        /// Gets or sets whether the directory is explicitly selected as watched.
        /// </summary>
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

        /// <summary>
        /// Gets the icon kind for the file system entry, displaying a folder or file icon.
        /// </summary>
        public MaterialIconKind IconKind =>
            IsDirectory ? MaterialIconKind.FolderOutline : MaterialIconKind.FileOutline;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for a specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed, automatically inferred if not specified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}