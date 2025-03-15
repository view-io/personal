// View.Personal.UIHandlers/ChatUIHandlers.cs

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

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
    using Avalonia.Media;
    using Classes;
    using Services;

    public static class ChatUIHandlers
    {
        public static async void SendMessage_Click(object sender, RoutedEventArgs e, Window window,
            List<ChatMessage> conversationHistory, Func<string, Action<string>, Task<string>> getAIResponse)
        {
            Console.WriteLine("[INFO] SendMessage_Click triggered. Sending user prompt to AI...");
            var inputBox = window.FindControl<TextBox>("ChatInputBox");
            var conversationContainer = window.FindControl<StackPanel>("ConversationContainer");
            var scrollViewer = window.FindControl<ScrollViewer>("ChatScrollViewer");
            var spinner = window.FindControl<ProgressBar>("ChatSpinner");

            if (inputBox != null && !string.IsNullOrWhiteSpace(inputBox.Text))
            {
                // Add the user's new message to conversation history
                conversationHistory.Add(new ChatMessage
                {
                    Role = "user",
                    Content = inputBox.Text
                });

                // Update UI
                UpdateConversationWindow(conversationContainer, conversationHistory);
                scrollViewer?.ScrollToEnd();

                if (spinner != null) spinner.IsVisible = true;

                // Get AI response
                try
                {
                    // Create a placeholder ChatMessage for the assistant
                    var assistantMsg = new ChatMessage
                    {
                        Role = "assistant",
                        Content = "" // start empty
                    };
                    conversationHistory.Add(assistantMsg);
                    UpdateConversationWindow(conversationContainer, conversationHistory);
                }
                finally
                {
                    if (spinner != null) spinner.IsVisible = false;
                }

                // Clear the input
                inputBox.Text = string.Empty;
            }
            else
            {
                Console.WriteLine("[WARN] User tried to send an empty or null message.");
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
            conversationContainer?.Children.Clear();
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
            List<ChatMessage> conversationHistory)
        {
            if (conversationContainer != null)
            {
                // Clear existing messages
                conversationContainer.Children.Clear();

                // Add each message with appropriate background color
                foreach (var msg in conversationHistory)
                {
                    var messageBlock = new TextBlock
                    {
                        Text = msg.Content,
                        TextWrapping = TextWrapping.Wrap,
                        Padding = new Thickness(10),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Colors.Black)
                    };

                    var messageBorder = new Border
                    {
                        Background = msg.Role == "user"
                            ? new SolidColorBrush(Color.FromArgb(100, 173, 216, 230))
                            : new SolidColorBrush(Color.FromArgb(100, 144, 238, 144)),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(5, 2, 5, 2),
                        Child = messageBlock
                    };

                    conversationContainer.Children.Add(messageBorder);
                }
            }
        }
    }
}