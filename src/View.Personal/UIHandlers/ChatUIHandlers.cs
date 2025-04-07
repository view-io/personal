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

    /// <summary>
    /// Provides static event handlers and utility methods for managing the chat user interface.
    /// Contains methods for sending messages, handling user input, clearing conversations,
    /// downloading chat history, and updating the conversation display.
    /// </summary>
    public static class ChatUIHandlers
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Handles the click event for sending a message in a chat interface. 
        /// Processes the user's input, adds it to the conversation history, 
        /// requests a response from the AI, and updates the UI accordingly.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data associated with the click event.</param>
        /// <param name="window">The window containing the chat interface controls.</param>
        /// <param name="conversationHistory">The list of chat messages representing the conversation history.</param>
        /// <param name="getAIResponse">A function that takes a user prompt and a token callback action, and returns the AI's response asynchronously.</param>
        /// <returns>This method doesn't return a value as it's marked async void.</returns>
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

        /// <summary>
        /// Handles the click event for sending a test message in a chat interface.
        /// Processes the user's input, adds it to the current chat session, 
        /// manages chat history for the first message in a session,
        /// requests a response from the AI, and updates the UI accordingly.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data associated with the click event.</param>
        /// <param name="window">The window containing the chat interface controls (must be of type MainWindow).</param>
        /// <param name="conversationHistory">The list of chat messages representing the conversation history.</param>
        /// <param name="getAIResponse">A function that takes a user prompt and a token callback action, and returns the AI's response asynchronously.</param>
        /// <returns>This method doesn't return a value as it's marked async void.</returns>
        public static async void SendMessageTest_Click(
            object sender,
            RoutedEventArgs e,
            Window window,
            List<ChatMessage> conversationHistory,
            Func<string, Action<string>, Task<string>> getAIResponse)
        {
            Console.WriteLine("[INFO] SendMessageTest_Click triggered. Sending user prompt to AI...");

            // Cast the window to MainWindow
            var mainWindow = window as MainWindow;
            if (mainWindow == null)
            {
                Console.WriteLine("[ERROR] Window is not of type MainWindow.");
                return;
            }

            // Use the current chat session's message list for consistency
            var currentMessages = mainWindow._CurrentChatSession.Messages;

            // Retrieve UI controls
            var inputBox = mainWindow.FindControl<TextBox>("ChatInputBox");
            var conversationContainer = mainWindow.FindControl<StackPanel>("ConversationContainer");
            var scrollViewer = mainWindow.FindControl<ScrollViewer>("ChatScrollViewer");

            // Validate input
            if (inputBox == null || string.IsNullOrWhiteSpace(inputBox.Text))
            {
                Console.WriteLine("[WARN] User tried to send an empty or null message.");
                return;
            }

            // Process user input
            var userText = inputBox.Text.Trim();
            inputBox.Text = string.Empty;

            Console.WriteLine("[DEBUG] Before adding user message, current messages count: " + currentMessages.Count);
            var userMessage = new ChatMessage { Role = "user", Content = userText };
            currentMessages.Add(userMessage);

            // Handle first message in the session
            if (currentMessages.Count == 1)
            {
                Console.WriteLine("[DEBUG] First message in session, creating chat history item.");
                mainWindow._CurrentChatSession.Title = GetTitleFromMessage(userText);
                var chatHistoryList = mainWindow.FindControl<ListBox>("ChatHistoryList");
                if (chatHistoryList != null)
                {
                    var chatLabel = mainWindow.FindControl<TextBlock>("ChatHistoryText");
                    if (chatLabel != null) chatLabel.Foreground = new SolidColorBrush(Color.Parse("#6A6B6F"));
                    var newItem = new ListBoxItem
                    {
                        Content = mainWindow._CurrentChatSession.Title,
                        Tag = mainWindow._CurrentChatSession
                    };
                    chatHistoryList.Items.Add(newItem);
                    Console.WriteLine("[DEBUG] Added chat history item: " + mainWindow._CurrentChatSession.Title);
                }
                else
                {
                    Console.WriteLine("[ERROR] ChatHistoryList not found.");
                }
            }

            // Update UI with user message
            UpdateConversationWindow(conversationContainer, currentMessages, false, mainWindow);
            Console.WriteLine("[DEBUG] Added user message. ConversationContainer children count: " +
                              (conversationContainer?.Children.Count ?? 0));
            if (scrollViewer != null)
                Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);

            try
            {
                // Add placeholder for assistant response
                var assistantMsg = new ChatMessage { Role = "assistant", Content = "" };
                currentMessages.Add(assistantMsg);
                UpdateConversationWindow(conversationContainer, currentMessages, true, mainWindow); // Show spinner
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
                        UpdateConversationWindow(conversationContainer, currentMessages, false,
                            mainWindow); // Hide spinner
                    }
                    else
                    {
                        UpdateConversationWindow(conversationContainer, currentMessages, false,
                            mainWindow); // Update UI
                    }

                    if (scrollViewer != null)
                        Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
                });

                // Finalize assistant response
                if (!string.IsNullOrEmpty(finalResponse) && assistantMsg.Content != finalResponse)
                {
                    assistantMsg.Content = finalResponse;
                }
                else if (string.IsNullOrEmpty(assistantMsg.Content))
                {
                    assistantMsg.Content = "No response received from the AI.";
                    Console.WriteLine("[WARN] No content accumulated in assistant message.");
                }

                // Final UI update
                UpdateConversationWindow(conversationContainer, currentMessages, false, mainWindow);
                if (scrollViewer != null)
                    Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[ERROR] Exception in SendMessageTest_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
                if (currentMessages.Last().Role == "assistant")
                    currentMessages.Last().Content = $"Error: {ex.Message}";
                UpdateConversationWindow(conversationContainer, currentMessages, false, mainWindow);
                if (scrollViewer != null)
                    Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// Creates a title from a message by taking the first few words and adding an ellipsis if necessary.
        /// </summary>
        /// <param name="message">The full message text to extract a title from.</param>
        /// <param name="wordCount">The maximum number of words to include in the title. Defaults to 5.</param>
        /// <returns>A string containing the first wordCount words of the message followed by an ellipsis, or the full message if it contains fewer words than wordCount.</returns>
        public static string GetTitleFromMessage(string message, int wordCount = 5)
        {
            var words = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= wordCount)
                return message;
            return string.Join(" ", words.Take(wordCount)) + "...";
        }

        /// <summary>
        /// Handles key down events for the chat input box, triggering message sending when the Enter key is pressed.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Key event data associated with the key press.</param>
        /// <param name="window">The window containing the chat interface controls.</param>
        /// <param name="conversationHistory">The list of chat messages representing the conversation history.</param>
        /// <param name="getAIResponse">A function that takes a user prompt and a token callback action, and returns the AI's response asynchronously.</param>
        public static void ChatInputBox_KeyDown(object sender, KeyEventArgs e, Window window,
            List<ChatMessage> conversationHistory, Func<string, Action<string>, Task<string>> getAIResponse)
        {
            if (e.Key == Key.Enter)
            {
                SendMessageTest_Click(sender, new RoutedEventArgs(), window, conversationHistory, getAIResponse);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the click event for clearing the current chat session.
        /// Removes all messages from the current chat session, updates the chat history list,
        /// removes the session from application state, and updates the UI accordingly.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data associated with the click event.</param>
        /// <param name="window">The window containing the chat interface controls (must be of type MainWindow).</param>
        /// <param name="conversationHistory">The list of chat messages representing the conversation history.</param>
        public static void ClearChat_Click(object sender, RoutedEventArgs e, Window window,
            List<ChatMessage> conversationHistory)
        {
            // Cast the Window parameter to MainWindow to access its members
            var mainWindow = window as MainWindow;

            // Check if the cast succeeded and if there’s a current chat session
            if (mainWindow != null)
            {
                // Clear the messages in the current chat session
                mainWindow._CurrentChatSession.Messages.Clear();

                // Find the chat history list control in the UI
                var chatHistoryList = mainWindow.FindControl<ListBox>("ChatHistoryList");
                if (chatHistoryList != null)
                {
                    // Find the ListBoxItem associated with the current chat session
                    var itemToRemove = chatHistoryList.Items
                        .OfType<ListBoxItem>()
                        .FirstOrDefault(item => item.Tag == mainWindow._CurrentChatSession);

                    // Remove the item if found
                    if (itemToRemove != null)
                    {
                        chatHistoryList.Items.Remove(itemToRemove);
                        Console.WriteLine("[DEBUG] Removed chat history button for cleared session.");
                    }

                    // Check if the chat history list is now empty
                    if (chatHistoryList.Items.Count == 0)
                    {
                        // Find the "Chat History" TextBlock and set its color to transparent
                        var chatHistoryText = mainWindow.FindControl<TextBlock>("ChatHistoryText");
                        if (chatHistoryText != null)
                            chatHistoryText.Foreground = new SolidColorBrush(Color.Parse("Transparent"));
                    }
                }

                // Remove the chat session using the public method
                mainWindow.RemoveChatSession(mainWindow._CurrentChatSession);

                // Update the conversation window to reflect the cleared state
                var conversationContainer = mainWindow.FindControl<StackPanel>("ConversationContainer");
                if (conversationContainer != null)
                    UpdateConversationWindow(conversationContainer, new List<ChatMessage>(), false, mainWindow);

                // Set the current chat session to null after clearing
                // ToDo: Consider using a more explicit method to clear the session
                mainWindow._CurrentChatSession = null!;
            }
            else
            {
                // Log if there’s no session to clear
                Console.WriteLine("[INFO] No current chat session to clear.");
            }
        }

        /// <summary>
        /// Handles the click event for downloading the current chat conversation.
        /// Prompts the user to select a save location and saves the conversation history as a text file.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data associated with the click event.</param>
        /// <param name="window">The window containing the chat interface controls.</param>
        /// <param name="conversationHistory">The list of chat messages representing the conversation history to be saved.</param>
        /// <param name="fileBrowserService">Service for browsing the file system and selecting save locations.</param>
        /// <returns>This method doesn't return a value as it's marked async void.</returns>
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
        /// Updates the conversation window UI with the current chat messages.
        /// Clears the existing conversation display and recreates it with the current message history,
        /// applying appropriate styling to user and assistant messages, and optionally showing a spinner
        /// for the assistant's response in progress.
        /// </summary>
        /// <param name="conversationContainer">The StackPanel container that holds the conversation messages.</param>
        /// <param name="conversationHistory">The list of chat messages to display.</param>
        /// <param name="showSpinner">Boolean indicating whether to show a loading spinner for the assistant's response.</param>
        /// <param name="window">The window containing the chat interface controls.</param>
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
                    messageBlock.MaxWidth = 610;
                    messageContainer.Children.Add(messageBlock);

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

        #endregion

        #region Private-Methods

        #endregion
    }
}