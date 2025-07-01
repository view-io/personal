namespace View.Personal.Services
{
    using Avalonia.Threading;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using View.Personal.Controls;
    using View.Personal.Enums;

    /// <summary>
    /// Service for tracking and reporting file ingestion progress.
    /// </summary>
    public static class IngestionProgressService
    {
        private static string _currentFile = string.Empty;
        private static IngestionProgressPopup? _progressPopup;
        private static bool _isProcessing = false;
        private static DateTime _lastUpdateTime = DateTime.MinValue;
        private static readonly TimeSpan _autoHideDelay = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// Dictionary to track all files currently being processed and their progress information
        /// </summary>
        private static readonly ConcurrentDictionary<string, (string Status, double Progress)> _activeIngestions = 
            new ConcurrentDictionary<string, (string Status, double Progress)>();

        /// <summary>
        /// Initializes the progress service with a reference to the progress popup UI.
        /// </summary>
        /// <param name="progressPopup">The progress popup UI control.</param>
        public static void Initialize(IngestionProgressPopup progressPopup)
        {
            _progressPopup = progressPopup;
            _progressPopup.IsVisible = false;
        }

        /// <summary>
        /// Updates the current file being processed and its progress.
        /// </summary>
        /// <param name="filePath">The path of the file being processed.</param>
        /// <param name="status">The current status message.</param>
        /// <param name="progressPercentage">The progress percentage (0-100).</param>
        public static void UpdateCurrentFileProgress(string filePath, string status, double progressPercentage)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            
            _currentFile = filePath;
            _lastUpdateTime = DateTime.Now;
            _isProcessing = true;
            
            _activeIngestions[filePath] = (status, progressPercentage);

            if (_progressPopup != null)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _progressPopup.UpdateCurrentFileProgress(filePath, status, progressPercentage);
                    _progressPopup.IsVisible = _activeIngestions.Count > 0 || HasPendingFiles();
                });
            }
            
            var app = Avalonia.Application.Current as App;
            app?.Log(SeverityEnum.Info, $"Ingestion progress: {Path.GetFileName(filePath)} - {status} ({progressPercentage}%)");
        }

        /// <summary>
        /// Updates the list of pending files in the queue.
        /// </summary>
        public static void UpdatePendingFiles()
        {
            if (_progressPopup != null)
            {
                var pendingFiles = FileIngester.IngestionList
                    .Where(file => !_activeIngestions.ContainsKey(file))
                    .ToList();
                _lastUpdateTime = DateTime.Now;
                
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _progressPopup.UpdatePendingFiles(pendingFiles);
                    _progressPopup.IsVisible = _activeIngestions.Count > 0 || pendingFiles.Count > 0;
                });
                
                // Log pending files count
                if (pendingFiles.Count > 0)
                {
                    var app = Avalonia.Application.Current as App;
                    app?.Log(SeverityEnum.Info, $"Pending files in queue: {pendingFiles.Count}");
                }
            }
        }

        /// <summary>
        /// Starts tracking a file ingestion process.
        /// </summary>
        /// <param name="filePath">The path of the file being ingested.</param>
        public static void StartFileIngestion(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            
            _isProcessing = true;
            _activeIngestions[filePath] = ("Starting ingestion...", 0);
            UpdateCurrentFileProgress(filePath, "Starting ingestion...", 0);
            UpdatePendingFiles();
        }

        /// <summary>
        /// Updates the progress of the current file ingestion.
        /// </summary>
        /// <param name="status">The current status message.</param>
        /// <param name="progressPercentage">The progress percentage (0-100).</param>
        public static void UpdateProgress(string status, double progressPercentage)
        {
            if (!string.IsNullOrEmpty(_currentFile))
            {
                UpdateCurrentFileProgress(_currentFile, status, progressPercentage);
            }
        }

        /// <summary>
        /// Completes the current file ingestion process.
        /// </summary>
        public static void CompleteFileIngestion()
        {
            if (!string.IsNullOrEmpty(_currentFile))
            {
                // Remove the file from active ingestions
                _activeIngestions.TryRemove(_currentFile, out _);
                
                // If there are other active ingestions, update the current file to the next one
                if (_activeIngestions.Count > 0)
                {
                    var nextFile = _activeIngestions.Keys.First();
                    var (status, progress) = _activeIngestions[nextFile];
                    _currentFile = nextFile;
                }
                else
                {
                    _isProcessing = false;
                    _currentFile = string.Empty;
                }
                
                UpdatePendingFiles();
                
                if (!HasPendingFiles() && _activeIngestions.Count == 0)
                {
                    Task.Delay(_autoHideDelay).ContinueWith(_ => {
                        if (!_isProcessing && !HasPendingFiles() && _activeIngestions.Count == 0 && 
                            DateTime.Now - _lastUpdateTime >= _autoHideDelay)
                        {
                            Dispatcher.UIThread.InvokeAsync(() => {
                                if (_progressPopup != null)
                                {
                                    _progressPopup.IsVisible = false;
                                }
                            });
                        }
                    });
                }
            }
        }
        
        /// <summary>
        /// Completes a specific file ingestion process.
        /// </summary>
        /// <param name="filePath">The path of the file that completed ingestion.</param>
        public static void CompleteFileIngestion(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            
            _activeIngestions.TryRemove(filePath, out _);
            
            if (filePath == _currentFile)
            {
                if (_activeIngestions.Count > 0)
                {
                    var nextFile = _activeIngestions.Keys.First();
                    var (status, progress) = _activeIngestions[nextFile];
                    _currentFile = nextFile;
                }
                else
                {
                    _isProcessing = false;
                    _currentFile = string.Empty;
                }
            }
            
            UpdatePendingFiles();

            if (!HasPendingFiles() && _activeIngestions.Count == 0)
            {
                Task.Delay(_autoHideDelay).ContinueWith(_ => {
                    if (!_isProcessing && !HasPendingFiles() && _activeIngestions.Count == 0 && 
                        DateTime.Now - _lastUpdateTime >= _autoHideDelay)
                    {
                        Dispatcher.UIThread.InvokeAsync(() => {
                            if (_progressPopup != null)
                            {
                                _progressPopup.IsVisible = false;
                            }
                        });
                    }
                });
            }
        }
        
        /// <summary>
        /// Gets all currently active ingestion files and their progress.
        /// </summary>
        /// <returns>A dictionary of file paths and their progress information.</returns>
        public static IReadOnlyDictionary<string, (string Status, double Progress)> GetActiveIngestions()
        {
            return _activeIngestions;
        }
        
        /// <summary>
        /// Checks if there are any pending files in the ingestion queue.
        /// </summary>
        /// <returns>True if there are pending files, false otherwise.</returns>
        private static bool HasPendingFiles()
        {
            return FileIngester.IngestionList.Count > 0;
        }
    }
}