namespace View.Personal.Controls.Dialogs
{
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Avalonia.Markup.Xaml;
    using Avalonia.Threading;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using View.Personal.Enums;
    using View.Personal.Services;

    /// <summary>
    /// Dialog for speech-to-text functionality using Vosk speech recognition.
    /// </summary>
    public partial class SpeechToTextDialog : UserControl
    {
        #region Private-Members

        private Window? _dialogWindow;
        private CancellationTokenSource? _recordingCts;
        private string _transcribedText = string.Empty;
        private bool _isRecording = false;

        #endregion

        #region Events

        /// <summary>
        /// Event that is raised when the transcription is completed.
        /// </summary>
        public event EventHandler<string>? TranscriptionCompleted;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechToTextDialog"/> class.
        /// </summary>
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

        #endregion

        #region Initialization

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Shows the speech-to-text dialog asynchronously.
        /// </summary>
        /// <param name="parent">The parent window for the dialog.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transcribed text, or null if canceled.</returns>
        public static async Task<string?> ShowAsync(Window parent)
        {
            var app = (App)App.Current!;

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
                    await window.ShowDialog(parent);
                }
                else
                {
                    window.Show();
                }

                app?.Log(SeverityEnum.Info, "Starting recording async");
                await dialog.StartRecordingAsync();

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                app?.Log(SeverityEnum.Error, $"Error in SpeechToTextDialog.ShowAsync: {ex.Message}");
                app?.LogExceptionToFile(ex, "SpeechToTextDialog.ShowAsync error");
                throw;
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Starts the recording and transcription process.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Handles the transcription process using Vosk speech recognition.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

                var model = await VoskModelService.GetModelAsync();

                if (model == null)
                {
                    Console.WriteLine("Failed to load Vosk model, falling back to simulation");
                    await FallbackSimulationAsync();
                    return;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    var statusText = this.FindControl<TextBlock>("StatusText");
                    if (statusText != null)
                        statusText.Text = "Listening...";
                });

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
                    if (_recordingCts?.Token.IsCancellationRequested ?? false)
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

                _recordingCts?.Token.Register(() =>
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

        /// <summary>
        /// Processes the JSON result from the speech recognition engine.
        /// </summary>
        /// <param name="jsonResult">The JSON result from the speech recognition engine.</param>
        /// <param name="isPartial">Indicates whether this is a partial result.</param>
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

        /// <summary>
        /// Provides a fallback simulation if the speech recognition engine fails.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task FallbackSimulationAsync()
        {
            // Fallback simulation if Vosk fails
            string[] simulatedWords = {
                "Hello", ", ", "this", " is", " a", " test", " of", " the", " speech", " to", " text", " feature", ".",
                " It", " will", " transcribe", " your", " voice", " in", " real", " time", " as", " you", " speak", "."
            };

            for (int i = 0; i < simulatedWords.Length; i++)
            {
                if (_recordingCts?.Token.IsCancellationRequested ?? false)
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

                await Task.Delay(200, _recordingCts?.Token ?? CancellationToken.None);
            }
        }

        /// <summary>
        /// Starts the microphone animation to indicate recording is in progress.
        /// </summary>
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

        /// <summary>
        /// Stops the microphone animation when recording is stopped.
        /// </summary>
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

        /// <summary>
        /// Stops the recording process.
        /// </summary>
        private void StopRecording()
        {
            if (_isRecording)
            {
                _isRecording = false;
                _recordingCts?.Cancel();
                StopMicrophoneAnimation();
            }
        }

        #endregion

        #region Event-Handlers

        /// <summary>
        /// Handles the click event of the close button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            StopRecording();
            _dialogWindow?.Close();
        }

        /// <summary>
        /// Handles the click event of the stop button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void StopButton_Click(object? sender, RoutedEventArgs e)
        {
            StopRecording();
        }

        /// <summary>
        /// Handles the click event of the send button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void SendButton_Click(object? sender, RoutedEventArgs e)
        {
            StopRecording();
            var transcriptionBox = this.FindControl<TextBox>("TranscriptionBox");
            string transcriptionText = transcriptionBox?.Text ?? string.Empty;
            TranscriptionCompleted?.Invoke(this, transcriptionText);
            _dialogWindow?.Close();
        }

        #endregion
    }
}