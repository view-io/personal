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
    using Classes;
    using Services;

    /// <summary>
    /// Provides event handlers and utility methods for managing the chat user interface.
    /// </summary>
    public static class ChatUIHandlers
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static ProgressBar _assistantSpinner;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Handles the click event for sending a message, processing user input, updating the UI, and retrieving an AI response.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The routed event arguments.</param>
        /// <param name="window">The window containing the chat controls.</param>
        /// <param name="conversationHistory">The list of ChatMessage objects representing the conversation history.</param>
        /// <param name="getAIResponse">A function that retrieves the AI response given user input and a token callback.</param>
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

            UpdateConversationWindow(conversationContainer, conversationHistory, false);
            scrollViewer?.ScrollToEnd();

            try
            {
                var assistantMsg = new ChatMessage
                {
                    Role = "assistant",
                    Content = ""
                };
                conversationHistory.Add(assistantMsg);
                UpdateConversationWindow(conversationContainer, conversationHistory, true); // Show spinner
                scrollViewer?.ScrollToEnd();

                Console.WriteLine("[DEBUG] Calling GetAIResponse...");
                var finalResponse = await getAIResponse(userText, (tokenChunk) =>
                {
                    Console.WriteLine($"[DEBUG] Received token chunk: '{tokenChunk}'");
                    assistantMsg.Content += tokenChunk;
                    UpdateConversationWindow(conversationContainer, conversationHistory, true);
                    scrollViewer?.ScrollToEnd();
                });

                Console.WriteLine($"[DEBUG] Final response from GetAIResponse: '{finalResponse}'");
                if (!string.IsNullOrEmpty(finalResponse) && assistantMsg.Content != finalResponse)
                {
                    assistantMsg.Content = finalResponse; // Ensure final response is set
                }
                else if (string.IsNullOrEmpty(assistantMsg.Content))
                {
                    assistantMsg.Content = "No response received from the AI.";
                    Console.WriteLine("[WARN] No content accumulated in assistant message.");
                }

                UpdateConversationWindow(conversationContainer, conversationHistory, false); // Hide spinner
                scrollViewer?.ScrollToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in SendMessage_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (conversationHistory.Last().Role == "assistant")
                    conversationHistory.Last().Content = $"Error: {ex.Message}";
                UpdateConversationWindow(conversationContainer, conversationHistory, false);
                scrollViewer?.ScrollToEnd();
            }
        }

        /// <summary>
        /// Handles the key down event for the chat input box, triggering message sending on Enter key press.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The key event arguments.</param>
        /// <param name="window">The window containing the chat controls.</param>
        /// <param name="conversationHistory">The list of ChatMessage objects representing the conversation history.</param>
        /// <param name="getAIResponse">A function that retrieves the AI response given user input and a token callback.</param>
        public static void ChatInputBox_KeyDown(object sender, KeyEventArgs e, Window window,
            List<ChatMessage> conversationHistory, Func<string, Action<string>, Task<string>> getAIResponse)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage_Click(sender, new RoutedEventArgs(), window, conversationHistory, getAIResponse);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the click event for clearing the chat, resetting the conversation history and UI.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The routed event arguments.</param>
        /// <param name="window">The window containing the chat controls.</param>
        /// <param name="conversationHistory">The list of ChatMessage objects representing the conversation history to clear.</param>
        public static void ClearChat_Click(object sender, RoutedEventArgs e, Window window,
            List<ChatMessage> conversationHistory)
        {
            Console.WriteLine("[INFO] Clearing chat history...");
            conversationHistory.Clear();
            var conversationContainer = window.FindControl<StackPanel>("ConversationContainer");
            conversationContainer?.Children.Clear();
            _assistantSpinner = null; // Reset spinner
        }

        /// <summary>
        /// Handles the click event for downloading the chat history to a file selected by the user.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The routed event arguments.</param>
        /// <param name="window">The window containing the chat controls.</param>
        /// <param name="conversationHistory">The list of ChatMessage objects representing the conversation history to save.</param>
        /// <param name="fileBrowserService">The service used to browse for a file save location.</param>
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

        /// <summary>
        /// Updates the conversation window UI by rendering the chat history with styled message blocks.
        /// </summary>
        /// <param name="conversationContainer">The StackPanel control where chat messages are displayed.</param>
        /// <param name="conversationHistory">The list of ChatMessage objects to render in the conversation window.</param>
        public static void UpdateConversationWindow(StackPanel? conversationContainer,
            List<ChatMessage> conversationHistory, bool showSpinner)
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
                        Margin = new Thickness(10, 0, 0, 0)
                    };

                    var messageBlock = new TextBlock
                    {
                        Text = string.IsNullOrEmpty(msg.Content) ? "" : msg.Content,
                        TextWrapping = TextWrapping.Wrap,
                        Padding = new Thickness(10),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.Parse("#1A1C1E"))
                    };

                    var messageContainer = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(0, 0, 0, 20)
                    };

                    messageContainer.Children.Add(labelBlock);
                    messageContainer.Children.Add(messageBlock);

                    if (msg.Role == "assistant" && msg == conversationHistory.Last() && showSpinner)
                    {
                        var spinner = new ProgressBar
                        {
                            IsIndeterminate = true,
                            Width = 100,
                            Margin = new Thickness(10, 5, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Left
                        };
                        messageContainer.Children.Add(spinner);
                    }

                    conversationContainer.Children.Add(messageContainer);
                }
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}