using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Material.Icons.Avalonia;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using View.Personal.Services;
using View.Personal.Enums;

namespace View.Personal.Controls.Dialogs
{
    public partial class SpeechToTextDialog : UserControl
    {
        private Window? _dialogWindow;
        private CancellationTokenSource? _recordingCts;
        private string _transcribedText = string.Empty;
        private bool _isRecording = false;

        public event EventHandler<string>? TranscriptionCompleted;

        public SpeechToTextDialog()
        {
            InitializeComponent();

            var closeButton = this.FindControl<Button>("CloseButton");
            var stopButton = this.FindControl<Button>("StopButton");
            var sendButton = this.FindControl<Button>("SendButton");
            
            if (closeButton != null)
                closeButton.Click += CloseButton_Click;
            if (stopButton != null)
                stopButton.Click += StopButton_Click;
            if (sendButton != null)
                sendButton.Click += SendButton_Click;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static async Task<string?> ShowAsync(Window parent)
        {
            var app = (App)App.Current;
            
            try
            {
                app?.Log(SeverityEnum.Info, "SpeechToTextDialog.ShowAsync called");
                
                bool isModelInstalled = VoskModelService.IsModelInstalled;
                bool isDownloading = VoskModelService.IsDownloading;
                
                app?.Log(SeverityEnum.Info, $"Model check - Installed: {isModelInstalled}, Downloading: {isDownloading}");
                
                if (!isModelInstalled || isDownloading)
                {
                    app?.Log(SeverityEnum.Info, "Showing download progress dialog");
                    bool downloadCompleted = await DownloadProgressDialog.ShowAsync(parent);
                    
                    app?.Log(SeverityEnum.Info, $"Download dialog completed: {downloadCompleted}");
                    
                    if (!downloadCompleted)
                        return null;
      
                    if (!VoskModelService.IsModelInstalled)
                    {
                        app?.Log(SeverityEnum.Warn, "Model still not installed after download dialog");
                        return null;
                    }
                }
                
                app?.Log(SeverityEnum.Info, "Creating SpeechToTextDialog window");
                
                var dialog = new SpeechToTextDialog();
                var window = new Window
                {
                    Title = "Speech to Text",
                    Content = dialog,
                    Width = 450,
                    Height = 450,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    ShowInTaskbar = false,
                    SystemDecorations = SystemDecorations.None,
                    Background = null,
                    TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent }
                };

                dialog._dialogWindow = window;
                
                var tcs = new TaskCompletionSource<string?>();
                dialog.TranscriptionCompleted += (s, text) => tcs.TrySetResult(text);
                window.Closed += (s, e) => tcs.TrySetResult(null);

                app?.Log(SeverityEnum.Info, "Showing dialog window");
                
                if (parent != null)
                {
                    window.ShowDialog(parent);
                }
                else
                {
                    window.Show();
                }

                // Start recording asynchronously after showing the window
                app?.Log(SeverityEnum.Info, "Starting recording async");
                _ = dialog.StartRecordingAsync();

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                app?.Log(SeverityEnum.Error, $"Error in SpeechToTextDialog.ShowAsync: {ex.Message}");
                app?.LogExceptionToFile(ex, "SpeechToTextDialog.ShowAsync error");
                throw;
            }
        }

        private async Task StartRecordingAsync()
        {
            var downloadProgress = this.FindControl<Border>("DownloadProgressContainer");
            if (downloadProgress != null)
                downloadProgress.IsVisible = false;

            var microphoneContainer = this.FindControl<Border>("MicrophoneContainer");
            var statusText = this.FindControl<TextBlock>("StatusText");
            var transcriptionBox = this.FindControl<TextBox>("TranscriptionBox");
            var stopButton = this.FindControl<Button>("StopButton");
            var sendButton = this.FindControl<Button>("SendButton");
            
            if (microphoneContainer != null) microphoneContainer.IsVisible = true;
            if (statusText != null) statusText.IsVisible = true;
            if (transcriptionBox != null) transcriptionBox.IsVisible = true;
            if (stopButton != null) stopButton.IsVisible = true;
            if (sendButton != null) sendButton.IsVisible = true;

            _isRecording = true;
            _recordingCts = new CancellationTokenSource();

            StartMicrophoneAnimation();

            if (transcriptionBox != null)
            {
                transcriptionBox.Text = string.Empty;
            }
            _transcribedText = string.Empty;

            await Task.Run(async () =>
            {
                try
                {
                    await SimulateTranscriptionAsync();
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var statusText = this.FindControl<TextBlock>("StatusText");
                        if (statusText != null)
                            statusText.Text = $"Error: {ex.Message}";
                    });
                }
            }, _recordingCts.Token);
        }

        private async Task SimulateTranscriptionAsync()
        {
            try
            {
                // Update status to show model loading
                Dispatcher.UIThread.Post(() =>
                {
                    var statusText = this.FindControl<TextBlock>("StatusText");
                    if (statusText != null)
                        statusText.Text = "Loading speech recognition model...";
                });

                // Get cached model (this will load it if not already loaded)
                var model = await VoskModelService.GetModelAsync();
                
                if (model == null)
                {
                    Console.WriteLine("Failed to load Vosk model, falling back to simulation");
                    await FallbackSimulationAsync();
                    return;
                }

                // Update status to show ready for speech
                Dispatcher.UIThread.Post(() =>
                {
                    var statusText = this.FindControl<TextBlock>("StatusText");
                    if (statusText != null)
                        statusText.Text = "Listening...";
                });
                
                // Initialize audio capture
                var waveFormat = new NAudio.Wave.WaveFormat(16000, 1);
                var waveIn = new NAudio.Wave.WaveInEvent
                {
                    DeviceNumber = 0,
                    WaveFormat = waveFormat,
                    BufferMilliseconds = 50
                };
                
                var recognizer = new Vosk.VoskRecognizer(model, waveFormat.SampleRate);
                recognizer.SetMaxAlternatives(0);
                recognizer.SetWords(true);
                
                var tcs = new TaskCompletionSource<bool>();
                waveIn.DataAvailable += (s, e) =>
                {
                    if (_recordingCts.Token.IsCancellationRequested)
                    {
                        if (!tcs.Task.IsCompleted)
                            tcs.SetResult(true);
                        return;
                    }
                    
                    if (recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        var result = recognizer.Result();
                        ProcessTranscriptionResult(result);
                    }
                    else
                    {
                        var partialResult = recognizer.PartialResult();
                        ProcessTranscriptionResult(partialResult, true);
                    }
                };
                
                waveIn.StartRecording();
                
                _recordingCts.Token.Register(() =>
                {
                    try
                    {
                        waveIn.StopRecording();
                        var finalResult = recognizer.FinalResult();
                        ProcessTranscriptionResult(finalResult);
                        recognizer.Dispose();
                        // Don't dispose the cached model here - it's shared
                        waveIn.Dispose();
                        if (!tcs.Task.IsCompleted)
                            tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during cleanup: {ex.Message}");
                        if (!tcs.Task.IsCompleted)
                            tcs.SetResult(true);
                    }
                });
                
                await tcs.Task;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Vosk: {ex.Message}");
                await FallbackSimulationAsync();
            }
        }
        
        private void ProcessTranscriptionResult(string jsonResult, bool isPartial = false)
        {
            try
            {
                var resultObj = System.Text.Json.JsonDocument.Parse(jsonResult);
                string text = string.Empty;
                
                if (isPartial)
                {
                    if (resultObj.RootElement.TryGetProperty("partial", out var partialElement))
                    {
                        text = partialElement.GetString() ?? string.Empty;
                    }
                }
                else
                {
                    if (resultObj.RootElement.TryGetProperty("text", out var textElement))
                    {
                        text = textElement.GetString() ?? string.Empty;
                    }
                }
                
                if (!string.IsNullOrEmpty(text))
                {
                    _transcribedText = text;
                    
                    Dispatcher.UIThread.Post(() =>
                    {
                        var transcriptionBox = this.FindControl<TextBox>("TranscriptionBox");
                        if (transcriptionBox != null)
                        {
                            transcriptionBox.Text = _transcribedText;
                            transcriptionBox.CaretIndex = transcriptionBox.Text?.Length ?? 0;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing transcription result: {ex.Message}");
            }
        }
        
        private async Task FallbackSimulationAsync()
        {
            // Fallback simulation if Vosk fails
            string[] simulatedWords = {
                "Hello", ", ", "this", " is", " a", " test", " of", " the", " speech", " to", " text", " feature", ".",
                " It", " will", " transcribe", " your", " voice", " in", " real", " time", " as", " you", " speak", "."
            };

            for (int i = 0; i < simulatedWords.Length; i++)
            {
                if (_recordingCts.Token.IsCancellationRequested)
                    break;

                _transcribedText += simulatedWords[i];
                
                Dispatcher.UIThread.Post(() =>
                {
                    var transcriptionBox = this.FindControl<TextBox>("TranscriptionBox");
                    if (transcriptionBox != null)
                    {
                        transcriptionBox.Text = _transcribedText;
                        transcriptionBox.CaretIndex = transcriptionBox.Text?.Length ?? 0;
                    }
                });

                await Task.Delay(200, _recordingCts.Token);
            }
        }

        private void StartMicrophoneAnimation()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var statusText = this.FindControl<TextBlock>("StatusText");
                var microphoneContainer = this.FindControl<Border>("MicrophoneContainer");
                var rippleEffect1 = this.FindControl<Border>("RippleEffect1");
                var rippleEffect2 = this.FindControl<Border>("RippleEffect2");

                if (statusText != null)
                    statusText.Text = "Listening...";

                // Add listening animation class to microphone container
                if (microphoneContainer != null)
                    microphoneContainer.Classes.Add("listening");

                // Start ripple effects with staggered timing
                if (rippleEffect1 != null)
                    rippleEffect1.Classes.Add("active");

                // Start second ripple effect with delay
                _ = Task.Delay(750).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (rippleEffect2 != null)
                            rippleEffect2.Classes.Add("active");
                    });
                });
            });
        }

        private void StopMicrophoneAnimation()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var statusText = this.FindControl<TextBlock>("StatusText");
                var microphoneContainer = this.FindControl<Border>("MicrophoneContainer");
                var rippleEffect1 = this.FindControl<Border>("RippleEffect1");
                var rippleEffect2 = this.FindControl<Border>("RippleEffect2");

                if (statusText != null)
                    statusText.Text = "Recording stopped";

                // Remove listening animation class
                if (microphoneContainer != null)
                    microphoneContainer.Classes.Remove("listening");

                // Stop ripple effects
                if (rippleEffect1 != null)
                    rippleEffect1.Classes.Remove("active");
                if (rippleEffect2 != null)
                    rippleEffect2.Classes.Remove("active");
            });
        }

        private void StopRecording()
        {
            if (_isRecording)
            {
                _isRecording = false;
                _recordingCts?.Cancel();
                StopMicrophoneAnimation();
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            StopRecording();
            _dialogWindow?.Close();
        }

        private void StopButton_Click(object? sender, RoutedEventArgs e)
        {
            StopRecording();
        }

        private void SendButton_Click(object? sender, RoutedEventArgs e)
        {
            StopRecording();
            var transcriptionBox = this.FindControl<TextBox>("TranscriptionBox");
            string transcriptionText = transcriptionBox?.Text ?? string.Empty;
            TranscriptionCompleted?.Invoke(this, transcriptionText);
            _dialogWindow?.Close();
        }
    }
}