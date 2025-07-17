using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using View.Personal.Services;

namespace View.Personal.Controls.Dialogs
{
    public partial class DownloadProgressDialog : UserControl
    {
        private Window? _dialogWindow;
        private System.Timers.Timer? _progressCheckTimer;
        private bool _downloadCompleted = false;

        public event EventHandler? DownloadCompleted;

        public DownloadProgressDialog()
        {
            InitializeComponent();
            
            if (this.FindControl<Button>("CloseButton") is Button closeButton)
                closeButton.Click += CloseButton_Click;
                
            if (this.FindControl<Button>("ContinueInBackgroundButton") is Button continueButton)
                continueButton.Click += ContinueInBackgroundButton_Click;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static async Task<bool> ShowAsync(Window parent)
        {
            var dialog = new DownloadProgressDialog();
            var window = new Window
            {
                Title = "Speech to Text Model Download",
                Content = dialog,
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
                SystemDecorations = SystemDecorations.None,
                Background = null,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent }
            };

            dialog._dialogWindow = window;
            
            dialog.StartProgressCheckTimer();

            var tcs = new TaskCompletionSource<bool>();
            dialog.DownloadCompleted += (s, e) => tcs.TrySetResult(true);
            window.Closed += (s, e) => tcs.TrySetResult(dialog._downloadCompleted);

            if (parent != null)
            {
                window.ShowDialog(parent);
            }
            else
            {
                window.Show();
            }

            return await tcs.Task;
        }

        private void StartProgressCheckTimer()
        {
            _progressCheckTimer?.Dispose();
            _progressCheckTimer = new System.Timers.Timer(500);
            _progressCheckTimer.Elapsed += (s, e) =>
            {
                if (VoskModelService.IsDownloading)
                {
                    UpdateProgress(VoskModelService.DownloadProgress);
                }
                else if (VoskModelService.IsModelInstalled)
                {
                    _downloadCompleted = true;
                    _progressCheckTimer?.Stop();
                    _progressCheckTimer?.Dispose();
                    Dispatcher.UIThread.Post(() => 
                    {
                        DownloadCompleted?.Invoke(this, EventArgs.Empty);
                        _dialogWindow?.Close();
                    });
                }
            };
            _progressCheckTimer.Start();
        }

        private void UpdateProgress(float progress)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (this.FindControl<ProgressBar>("DownloadProgressBar") is ProgressBar progressBar)
                    progressBar.Value = progress;
                    
                if (this.FindControl<TextBlock>("DownloadStatusText") is TextBlock statusText)
                    statusText.Text = $"Downloading Vosk model... {progress:F0}%";
            });
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            _progressCheckTimer?.Stop();
            _progressCheckTimer?.Dispose();
            _dialogWindow?.Close();
        }

        private void ContinueInBackgroundButton_Click(object? sender, RoutedEventArgs e)
        {
            _progressCheckTimer?.Stop();
            _progressCheckTimer?.Dispose();
            _dialogWindow?.Close();
        }
    }
}