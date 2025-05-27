namespace View.Personal.Classes
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents a view model for a file, containing metadata such as name, creation date, and file properties.
    /// </summary>
    public class FileViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isChecked;

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the date and time the file was created.
        /// </summary>
        public string? CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// Gets or sets the file size.
        /// </summary>
        public string? ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the node GUID.
        /// </summary>
        public Guid NodeGuid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file is selected in the UI.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the file is checked in the UI.
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}