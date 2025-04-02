namespace View.Personal.UIHandlers
{
    using Avalonia;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Layout;
    using Avalonia.Media;
    using Avalonia.Threading;
    using Classes;
    using Services;

    public static class ChatUIHandlers
    {
        public static async void SendMessage_Click(
            object sender,
            RoutedEventArgs e,
            Window window,
            List<ChatMessage> conversationHistory,
            Func<string, Action<string>, Task<string>> getAIResponse)
        {
            Console.WriteLine("[INFO] SendMessage_Click triggered. Sending user prompt to AI...");

            var inputBox = window.FindControl<TextBox>("ChatInputBox");
            var conversationContainer = window.FindControl<StackPanel>("ConversationContainer");
            var scrollViewer = window.FindControl<ScrollViewer>("ChatScrollViewer");

            if (inputBox == null || string.IsNullOrWhiteSpace(inputBox.Text))
            {
                Console.WriteLine("[WARN] User tried to send an empty or null message.");
                return;
            }

            var userText = inputBox.Text.Trim();
            inputBox.Text = string.Empty;

            conversationHistory.Add(new ChatMessage
            {
                Role = "user",
                Content = userText
            });

            UpdateConversationWindow(conversationContainer, conversationHistory, false, window);
            Console.WriteLine("[DEBUG] Added user message. ConversationContainer children count: " +
                              (conversationContainer?.Children.Count ?? 0));
            if (scrollViewer != null)
                Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);

            try
            {
                var assistantMsg = new ChatMessage
                {
                    Role = "assistant",
                    Content = ""
                };
                conversationHistory.Add(assistantMsg);
                UpdateConversationWindow(conversationContainer, conversationHistory, true,
                    window); // Show spinner initially
                if (scrollViewer != null)
                    Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);

                Console.WriteLine("[DEBUG] Calling GetAIResponse...");
                var firstTokenReceived = false;
                var finalResponse = await getAIResponse(userText, (tokenChunk) =>
                {
                    assistantMsg.Content += tokenChunk;
                    if (!firstTokenReceived)
                    {
                        firstTokenReceived = true;
                        UpdateConversationWindow(conversationContainer, conversationHistory, false,
                            window); // Hide spinner on first token
                    }
                    else
                    {
                        UpdateConversationWindow(conversationContainer, conversationHistory, false,
                            window); // Update without spinner
                    }

                    if (scrollViewer != null)
                        Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
                });

                Console.WriteLine($"[DEBUG] Final response from GetAIResponse: '{finalResponse}'");
                if (!string.IsNullOrEmpty(finalResponse) && assistantMsg.Content != finalResponse)
                {
                    assistantMsg.Content = finalResponse;
                }
                else if (string.IsNullOrEmpty(assistantMsg.Content))
                {
                    assistantMsg.Content = "No response received from the AI.";
                    Console.WriteLine("[WARN] No content accumulated in assistant message.");
                }

                UpdateConversationWindow(conversationContainer, conversationHistory, false,
                    window); // Final update without spinner
                if (scrollViewer != null)
                    Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in SendMessage_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (conversationHistory.Last().Role == "assistant")
                    conversationHistory.Last().Content = $"Error: {ex.Message}";
                UpdateConversationWindow(conversationContainer, conversationHistory, false, window);
                if (scrollViewer != null)
                    Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
            }
        }

        public static void ChatInputBox_KeyDown(object sender, KeyEventArgs e, Window window,
            List<ChatMessage> conversationHistory, Func<string, Action<string>, Task<string>> getAIResponse)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage_Click(sender, new RoutedEventArgs(), window, conversationHistory, getAIResponse);
                e.Handled = true;
            }
        }

        public static void ClearChat_Click(object sender, RoutedEventArgs e, Window window,
            List<ChatMessage> conversationHistory)
        {
            Console.WriteLine("[INFO] Clearing chat history...");
            conversationHistory.Clear();
            var conversationContainer = window.FindControl<StackPanel>("ConversationContainer");
            UpdateConversationWindow(conversationContainer, conversationHistory, false, window);
        }

        public static async void DownloadChat_Click(object sender, RoutedEventArgs e, Window window,
            List<ChatMessage> conversationHistory, FileBrowserService fileBrowserService)
        {
            Console.WriteLine("[INFO] DownloadChat_Click triggered...");
            var filePath = await fileBrowserService.BrowseForChatHistorySaveLocation(window);

            if (!string.IsNullOrEmpty(filePath))
                try
                {
                    await File.WriteAllLinesAsync(filePath,
                        conversationHistory.Select(msg => $"{msg.Role}: {msg.Content}"));
                    Console.WriteLine($"[INFO] Chat history saved to {filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error saving chat history: {ex.Message}");
                }
            else
                Console.WriteLine("[WARN] No file path selected for chat history download.");
        }

        public static void UpdateConversationWindow(StackPanel? conversationContainer,
            List<ChatMessage> conversationHistory, bool showSpinner, Window window)
        {
            if (conversationContainer != null)
            {
                conversationContainer.Children.Clear();

                foreach (var msg in conversationHistory)
                {
                    var labelBlock = new TextBlock
                    {
                        Text = msg.Role == "user" ? "You" : "Assistant",
                        Foreground = new SolidColorBrush(Color.Parse("#464A4D")),
                        FontSize = 12,
                        FontWeight = FontWeight.Normal,
                        Margin = new Thickness(180, 0, 0, 0)
                    };

                    var messageBlock = new TextBlock
                    {
                        Text = string.IsNullOrEmpty(msg.Content) ? "" : msg.Content,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10, 0, 0, 0),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.Parse("#1A1C1E"))
                    };

                    var messageContainer = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(0, 0, 0, 20)
                    };

                    messageContainer.Children.Add(labelBlock);
                    messageBlock.TextWrapping = TextWrapping.Wrap;
                    messageBlock.MaxWidth = 610; // Match input box width for consistency
                    messageContainer.Children.Add(messageBlock);
                    Dispatcher.UIThread.Post(() =>
                    {
                        Console.WriteLine("[DEBUG] LabelBlock Bounds.X: " + labelBlock.Bounds.X);
                        Console.WriteLine("[DEBUG] MessageBlock Bounds.X: " + messageBlock.Bounds.X);
                    }, DispatcherPriority.Background);

                    if (msg.Role == "assistant" && msg == conversationHistory.Last() && showSpinner)
                    {
                        var spinner = new ProgressBar
                        {
                            IsIndeterminate = true,
                            Width = 100,
                            Margin = new Thickness(180, 5, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Left
                        };
                        messageContainer.Children.Add(spinner);
                    }

                    conversationContainer.Children.Add(messageContainer);
                }

                // Ensure scroll viewer scrolls to bottom when new messages are added
                var scrollViewer = window.FindControl<ScrollViewer>("ChatScrollViewer");
                if (scrollViewer != null && conversationHistory.Any())
                    Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
            }
        }
    }
}