namespace View.Personal.UIHandlers
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Layout;
    using Avalonia.Media;
    using Avalonia.Threading;
    using Classes;
    using Services;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using View.Personal.Controls.Renderer;
    using View.Personal.Enums;

    /// <summary>
    /// Provides static event handlers and utility methods for managing the chat user interface.
    /// Contains methods for sending messages, handling user input, clearing conversations,
    /// downloading chat history, and updating the conversation display.
    /// </summary>
    public static class ChatUIHandlers
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

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
            // Cast the window to MainWindow
            var mainWindow = window as MainWindow;
            if (mainWindow == null) return;

            var currentMessages = mainWindow.CurrentChatSession.Messages;
            var app = (App)App.Current;

            var inputBox = mainWindow.FindControl<TextBox>("ChatInputBox");
            var emptyInputBox = mainWindow.FindControl<TextBox>("EmptyChatInputBox");
            var conversationContainer = mainWindow.FindControl<StackPanel>("ConversationContainer");
            var scrollViewer = mainWindow.FindControl<ScrollViewer>("ChatScrollViewer");

            string userText = "";
            if (inputBox != null && !string.IsNullOrWhiteSpace(inputBox.Text))
            {
                userText = inputBox.Text.Trim();
                inputBox.Text = string.Empty;
            }
            else if (emptyInputBox != null && !string.IsNullOrWhiteSpace(emptyInputBox.Text))
            {
                userText = emptyInputBox.Text.Trim();
                emptyInputBox.Text = string.Empty;
            }
            else
            {
                app.Log(SeverityEnum.Warn, "User tried to send an empty or null message.");
                return;
            }

            // Process user input
            var userMessage = new ChatMessage { Role = "user", Content = userText };
            currentMessages.Add(userMessage);

            // Handle first message in the session
            if (currentMessages.Count == 1)
            {
                mainWindow.CurrentChatSession.Title = GetTitleFromMessage(userText);
                var chatHistoryList = mainWindow.FindControl<ComboBox>("ChatHistoryList");
                if (chatHistoryList != null)
                {
                    var newItem = new ListBoxItem
                    {
                        Content = mainWindow.CurrentChatSession.Title,
                        Tag = mainWindow.CurrentChatSession
                    };
                    chatHistoryList.Items.Add(newItem);
                    chatHistoryList.SelectedItem = newItem;
                }
            }

            // Update UI with user message
            UpdateConversationWindow(conversationContainer, currentMessages, false, mainWindow);
            if (scrollViewer != null)
                Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);

            try
            {
                var assistantMsg = new ChatMessage { Role = "assistant", Content = "" };
                currentMessages.Add(assistantMsg);
                UpdateConversationWindow(conversationContainer, currentMessages, true, mainWindow); // Show spinner
                if (scrollViewer != null)
                    Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);

                app.LogWithTimestamp(SeverityEnum.Debug, "Calling GetAIResponse...");
                var firstTokenReceived = false;
                var finalResponse = await getAIResponse(userText, (tokenChunk) =>
                {
                    assistantMsg.Content += tokenChunk;
                    if (!firstTokenReceived)
                    {
                        firstTokenReceived = true;
                        Dispatcher.UIThread.Post(() => {
                            UpdateConversationWindow(conversationContainer, currentMessages, false, mainWindow);
                            scrollViewer?.ScrollToEnd();
                        }, DispatcherPriority.Background);
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => {
                            UpdateConversationWindow(conversationContainer, currentMessages, false, mainWindow);
                            scrollViewer?.ScrollToEnd();
                        }, DispatcherPriority.Background);
                    }
                });

                // Finalize assistant response
                if (!string.IsNullOrEmpty(finalResponse) && assistantMsg.Content != finalResponse)
                {
                    assistantMsg.Content = finalResponse;
                }
                else if (string.IsNullOrEmpty(assistantMsg.Content))
                {
                    assistantMsg.Content = "No response received from the AI.";
                    app.LogWithTimestamp(SeverityEnum.Warn, "No content accumulated in assistant message.");
                }

                // Final UI update
                UpdateConversationWindow(conversationContainer, currentMessages, false, mainWindow);
                if (scrollViewer != null)
                    Dispatcher.UIThread.Post(() => scrollViewer.ScrollToEnd(), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                app.LogWithTimestamp(SeverityEnum.Error, $"Exception in SendMessageTest_Click: {ex.Message}\n\nStackTrace: {ex.StackTrace}");
                app?.LogExceptionToFile(ex, $"Exception in SendMessageTest_Click");
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
                mainWindow.CurrentChatSession.Messages.Clear();

                // Find the chat history list control in the UI
                var chatHistoryList = mainWindow.FindControl<ComboBox>("ChatHistoryList");
                if (chatHistoryList != null)
                {
                    // Find the ListBoxItem associated with the current chat session
                    var itemToRemove = chatHistoryList.Items
                        .OfType<ListBoxItem>()
                        .FirstOrDefault(item => item.Tag == mainWindow.CurrentChatSession);

                    // Remove the item if found
                    if (itemToRemove != null) chatHistoryList.Items.Remove(itemToRemove);

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
                mainWindow.RemoveChatSession(mainWindow.CurrentChatSession);

                // Update the conversation window to reflect the cleared state
                var conversationContainer = mainWindow.FindControl<StackPanel>("ConversationContainer");
                if (conversationContainer != null)
                    UpdateConversationWindow(conversationContainer, new List<ChatMessage>(), false, mainWindow);

                mainWindow.CurrentChatSession = new ChatSession();
                mainWindow.ChatSessions.Add(mainWindow.CurrentChatSession);
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
            var filePath = await fileBrowserService.BrowseForChatHistorySaveLocation(window);
            var app = (App)App.Current;
            if (!string.IsNullOrEmpty(filePath))
                try
                {
                    await File.WriteAllLinesAsync(filePath,
                        conversationHistory.Select(msg => $"{msg.Role}: {msg.Content}"));
                    app.Log(SeverityEnum.Info, $"Chat history saved to {filePath}");
                }
                catch (Exception ex)
                {
                    app.Log(SeverityEnum.Error, $"Error saving chat history: {ex.Message}");
                }
            else
                app.Log(SeverityEnum.Warn, "No file path selected for chat history download.");
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
                    var container = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(0, 0, 0, 20),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    var labelBlock = new TextBlock
                    {
                        Text = msg.Role == "user" ? "You" : "Assistant",
                        Foreground = new SolidColorBrush(Color.Parse("#464A4D")),
                        FontSize = 12,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(10, 0, 10, 2)
                    };

                    Control messageContent;
                    if (msg.Role == "assistant" && !string.IsNullOrEmpty(msg.Content))
                    {
                        messageContent = MarkdownRenderer.Render(msg.Content);
                    }
                    else
                    {
                        messageContent = new SelectableTextBlock
                        {
                            Text = msg.Content ?? "",
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.Parse("#1A1C1E"))
                        };
                    }

                    // Wrap message in a container with max width and center alignment
                    var messageWrapper = new Border
                    {
                        MaxWidth = 650,
                        Child = messageContent,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(10, 0, 10, 0)
                    };

                    container.Children.Add(labelBlock);
                    container.Children.Add(messageWrapper);

                    // Optional spinner
                    if (msg.Role == "assistant" && msg == conversationHistory.Last() && showSpinner)
                    {
                        container.Children.Add(new ProgressBar
                        {
                            IsIndeterminate = true,
                            Width = 70,
                            Margin = new Thickness(10, 5, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Left
                        });
                    }

                    var outerWrapper = new Grid
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(180, 0, 0, 0),
                        Width = 1000
                    };
                    outerWrapper.Children.Add(container);
                    conversationContainer.Children.Add(outerWrapper);

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