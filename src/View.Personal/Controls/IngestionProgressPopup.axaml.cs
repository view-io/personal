namespace View.Personal.Controls
{
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Avalonia.Markup.Xaml;
    using Avalonia.Threading;
    using Material.Icons;
    using Material.Icons.Avalonia;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using View.Personal.Classes;
    using View.Personal.Services;

    /// <summary>
    /// A collapsible popup control that displays real-time file ingestion progress and pending queue.
    /// </summary>
    public partial class IngestionProgressPopup : UserControl
    {
        private bool _isCollapsed = false;
        private MaterialIcon _collapseIcon;
        private Grid _contentPanel;
        private ItemsRepeater _pendingFilesRepeater;
        private ItemsRepeater _activeFilesRepeater;
        private TextBlock _pendingCountText;
        private TextBlock _activeCountText;
        private List<string> _pendingFiles = new List<string>();
        private List<(string FilePath, string Status, double Progress)> _activeFiles = new List<(string, string, double)>();
        private DateTime _lastActivity = DateTime.Now;

        /// <summary>
        /// Initializes a new instance of the <see cref="IngestionProgressPopup"/> class.
        /// </summary>
        public IngestionProgressPopup()
        {
            InitializeComponent();

            _collapseIcon = (this.FindControl<Button>("CollapseButton")!.Content as MaterialIcon)!;
            _contentPanel = this.FindControl<Grid>("ContentPanel")!;
            _pendingFilesRepeater = this.FindControl<ItemsRepeater>("PendingFilesRepeater")!;
            _activeFilesRepeater = this.FindControl<ItemsRepeater>("ActiveFilesRepeater")!;
            _pendingCountText = this.FindControl<TextBlock>("PendingCountText")!;
            _activeCountText = this.FindControl<TextBlock>("ActiveCountText")!;

            // Initialize with empty lists
            UpdatePendingFiles(new List<string>());
            UpdateActiveFiles(new List<(string, string, double)>());

            // Set initial visibility
            this.IsVisible = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Updates the current file being processed and its progress.
        /// </summary>
        /// <param name="filePath">The path of the file being processed.</param>
        /// <param name="status">The current status message.</param>
        /// <param name="progressPercentage">The progress percentage (0-100).</param>
        public void UpdateCurrentFileProgress(string filePath, string status, double progressPercentage)
        {
            _lastActivity = DateTime.Now;
            UpdateActiveFilesFromService();
        }

        /// <summary>
        /// Updates the list of pending files in the queue.
        /// </summary>
        /// <param name="pendingFiles">The list of file paths pending ingestion.</param>
        public void UpdatePendingFiles(List<string> pendingFiles)
        {
            _lastActivity = DateTime.Now;
            _pendingFiles = pendingFiles.Select(Path.GetFileName).Where(name => name != null).Select(name => name!).ToList();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _pendingFilesRepeater.ItemsSource = _pendingFiles;

                if (_pendingCountText != null)
                {
                    _pendingCountText.Text = string.Format(ResourceManagerService.GetString("PendingQueue"), _pendingFiles.Count);
                }
            });
            UpdateActiveFilesFromService();
        }


        /// <summary>
        /// Handles the click event for the collapse/expand button.
        /// </summary>
        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            _lastActivity = DateTime.Now;
            _isCollapsed = !_isCollapsed;

            if (_isCollapsed)
            {
                _contentPanel.IsVisible = false;
                _collapseIcon.Kind = MaterialIconKind.ChevronUp;
            }
            else
            {
                _contentPanel.IsVisible = true;
                _collapseIcon.Kind = MaterialIconKind.ChevronDown;
            }
        }

        /// <summary>
        /// Handles the click event for the cancel all button.
        /// </summary>
        private void CancelAllButton_Click(object sender, RoutedEventArgs e)
        {
            _lastActivity = DateTime.Now;
            IngestionProgressService.CancelAllFileIngestions();

        }

        /// <summary>
        /// Updates the list of active files being processed.
        /// </summary>
        /// <param name="activeFiles">The list of active files with their status and progress.</param>
        public void UpdateActiveFiles(List<(string FilePath, string Status, double Progress)> activeFiles)
        {
            _lastActivity = DateTime.Now;
            _activeFiles = activeFiles;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _activeFilesRepeater.ItemsSource = _activeFiles.Select(file => new ActiveFileViewModel
                {
                    FileName = Path.GetFileName(file.FilePath),
                    FilePath = file.FilePath,
                    Status = file.Status,
                    Progress = file.Progress
                }).ToList();

                if (_activeCountText != null)
                {
                    _activeCountText.Text = string.Format(ResourceManagerService.GetString("CurrentIngestion"), _activeFiles.Count);
                }
            });
        }

        /// <summary>
        /// Updates the active files list from the IngestionProgressService.
        /// </summary>
        private void UpdateActiveFilesFromService()
        {
            var activeIngestions = IngestionProgressService.GetActiveIngestions();
            var activeFiles = activeIngestions.Select(kv => (kv.Key, kv.Value.Status, kv.Value.Progress)).ToList();
            UpdateActiveFiles(activeFiles);
        }
    }
}