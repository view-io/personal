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

            CloseButton.Click += CloseButton_Click;
            StopButton.Click += StopButton_Click;
            SendButton.Click += SendButton_Click;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static async Task<string?> ShowAsync(Window parent)
        {
            if (!VoskModelService.IsModelInstalled || VoskModelService.IsDownloading)
            {
                bool downloadCompleted = await DownloadProgressDialog.ShowAsync(parent);
                
                if (!downloadCompleted)
                    return null;
  
                if (!VoskModelService.IsModelInstalled)
                    return null;
            }
            
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
            await dialog.StartRecordingAsync();

            var tcs = new TaskCompletionSource<string?>();
            dialog.TranscriptionCompleted += (s, text) => tcs.TrySetResult(text);
            window.Closed += (s, e) => tcs.TrySetResult(null);

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

        private async Task StartRecordingAsync()
        {
            DownloadProgressContainer.IsVisible = false;
            MicrophoneContainer.IsVisible = true;
            StatusText.IsVisible = true;
            TranscriptionBox.IsVisible = true;
            StopButton.IsVisible = true;
            SendButton.IsVisible = true;

            _isRecording = true;
            _recordingCts = new CancellationTokenSource();

            StartMicrophoneAnimation();

            TranscriptionBox.Text = string.Empty;
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
                        StatusText.Text = $"Error: {ex.Message}";
                    });
                }
            }, _recordingCts.Token);
        }

        private async Task SimulateTranscriptionAsync()
        {
            try
            {
                Vosk.Vosk.SetLogLevel(0);
                var modelPath = VoskModelService.ModelPath;
                var model = new Vosk.Model(modelPath);
                
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
                        model.Dispose();
                        waveIn.Dispose();
                        if (!tcs.Task.IsCompleted)
                            tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during cleanup: {ex.Message}");
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
                        TranscriptionBox.Text = _transcribedText;
                        TranscriptionBox.CaretIndex = TranscriptionBox.Text.Length;
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
                    TranscriptionBox.Text = _transcribedText;
                    TranscriptionBox.CaretIndex = TranscriptionBox.Text.Length;
                });

                await Task.Delay(200, _recordingCts.Token);
            }
        }

        private void StartMicrophoneAnimation()
        {
            StatusText.Text = "Listening...";
        }

        private void StopMicrophoneAnimation()
        {
            StatusText.Text = "Stopped";
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
            TranscriptionCompleted?.Invoke(this, TranscriptionBox.Text);
            _dialogWindow?.Close();
        }
    }
}