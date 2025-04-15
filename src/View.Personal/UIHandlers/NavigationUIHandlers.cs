namespace View.Personal.UIHandlers
{
    using Avalonia;
    using System;
    using System.Linq;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Avalonia.Media;
    using Helpers;
    using LiteGraph;

    /// <summary>
    /// Provides event handlers and utility methods for managing navigation in the user interface.
    /// </summary>
    public static class NavigationUIHandlers
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.


        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Handles the selection changed event for the navigation list, updating the visibility of UI panels accordingly.
        /// </summary>
        /// <param name="sender">The ListBox that triggered the selection change event.</param>
        /// <param name="e">The selection changed event arguments.</param>
        /// <param name="window">The window containing the navigation panels.</param>
        /// <param name="liteGraph">The LiteGraphClient instance for interacting with graph data.</param>
        /// <param name="tenantGuid">The GUID identifying the tenant.</param>
        /// <param name="graphGuid">The GUID identifying the graph.</param>
        public static void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e, Window window,
            LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid)
        {
            if (sender is ListBox listBox)
            {
                // Retrieve UI controls
                var chatHistoryList = window.FindControl<ListBox>("ChatHistoryList");
                var mainWindow = window as MainWindow;
                var dashboardPanel = window.FindControl<Border>("DashboardPanel");
                var settingsPanel2 = window.FindControl<StackPanel>("SettingsPanel2");
                var myFilesPanel = window.FindControl<StackPanel>("MyFilesPanel");
                var chatPanel = window.FindControl<Border>("ChatPanel");
                var consolePanel = window.FindControl<StackPanel>("ConsolePanel");
                var workspaceText = window.FindControl<TextBlock>("WorkspaceText");
                var mainContentArea = window.FindControl<Grid>("MainContentArea");
                var dataMonitorPanel = window.FindControl<StackPanel>("DataMonitorPanel");

                // Set main content area background to white
                if (mainContentArea != null)
                    mainContentArea.Background = new SolidColorBrush(Colors.White);

                // Hide all panels initially
                if (dashboardPanel != null) dashboardPanel.IsVisible = false;
                if (settingsPanel2 != null) settingsPanel2.IsVisible = false;
                if (myFilesPanel != null) myFilesPanel.IsVisible = false;
                if (chatPanel != null) chatPanel.IsVisible = false;
                if (consolePanel != null) consolePanel.IsVisible = false;
                if (workspaceText != null) workspaceText.IsVisible = false;
                if (dataMonitorPanel != null) dataMonitorPanel.IsVisible = false;

                if (listBox.SelectedItem is ListBoxItem selectedItem)
                {
                    var selectedTag = selectedItem.Tag?.ToString();
                    switch (selectedTag)
                    {
                        case "Files":
                            if (myFilesPanel != null)
                            {
                                myFilesPanel.IsVisible = true;
                                var filesDataGrid = window.FindControl<DataGrid>("FilesDataGrid");
                                var uploadFilesPanel = window.FindControl<Border>("UploadFilesPanel");
                                var fileOperationsPanel = window.FindControl<Grid>("FileOperationsPanel");

                                if (filesDataGrid != null && uploadFilesPanel != null && fileOperationsPanel != null)
                                {
                                    var uniqueFiles =
                                        MainWindowHelpers.GetDocumentNodes(liteGraph, tenantGuid, graphGuid);

                                    if (uniqueFiles.Any())
                                    {
                                        filesDataGrid.ItemsSource = uniqueFiles;
                                        uploadFilesPanel.IsVisible = false;
                                        filesDataGrid.IsVisible = true;
                                    }
                                    else
                                    {
                                        filesDataGrid.ItemsSource = null;
                                        filesDataGrid.IsVisible = false;
                                        fileOperationsPanel.IsVisible = false;
                                        uploadFilesPanel.IsVisible = true;
                                    }

                                    Console.WriteLine(
                                        $"[INFO] Loaded {uniqueFiles.Count()} unique files into MyFilesPanel.");
                                }
                            }

                            break;

                        case "Data Monitor":
                            mainWindow.ShowPanel("Data Monitor");
                            break;

                        case "Settings2":
                            if (settingsPanel2 != null) settingsPanel2.IsVisible = true;
                            break;

                        case "Console":
                            if (consolePanel != null) consolePanel.IsVisible = true;
                            break;
                    }

                    // Deselect chat history list when a navigation item is selected
                    if (chatHistoryList != null) chatHistoryList.SelectedIndex = -1;
                }
                else
                {
                    // No navigation item selected, show chat panel if possible, otherwise dashboard
                    if (mainWindow != null && chatPanel != null)
                        chatPanel.IsVisible = true;
                    else if (dashboardPanel != null) dashboardPanel.IsVisible = true;
                }
            }
        }

        /// <summary>
        /// Handles the selection changed event for the model provider combo box, updating settings and visibility.
        /// </summary>
        /// <param name="sender">The ComboBox that triggered the selection change event.</param>
        /// <param name="e">The selection changed event arguments.</param>
        /// <param name="window">The window containing the provider selection controls.</param>
        /// <param name="windowInitialized">A flag indicating whether the window has finished initializing.</param>
        public static void ModelProvider_SelectionChanged(object? sender, SelectionChangedEventArgs e, Window window,
            bool windowInitialized)
        {
            if (!windowInitialized) return;
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedProvider = selectedItem.Content?.ToString();
                Console.WriteLine($"[INFO] ModelProvider_SelectionChanged: {selectedProvider}");

                if (string.IsNullOrEmpty(selectedProvider))
                {
                    Console.WriteLine("[ERROR] Selected provider is null or empty.");
                    return;
                }

                var app = (App)Application.Current;
                app?.SaveSelectedProvider(selectedProvider);
            }
        }

        /// <summary>
        /// Handles the click event to navigate to the My Files panel in the UI.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The routed event arguments.</param>
        /// <param name="window">The window containing the navigation panels.</param>
        public static void NavigateToMyFiles_Click(object sender, RoutedEventArgs e, Window window)
        {
            NavigateToPanel(window, "My Files");
        }

        /// <summary>
        /// Handles the click event to navigate to the Chat panel in the UI.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The routed event arguments.</param>
        /// <param name="window">The window containing the navigation panels.</param>
        public static void NavigateToChat_Click(object sender, RoutedEventArgs e, Window window)
        {
            NavigateToPanel(window, "Chat");
        }

        /// <summary>
        /// Navigates to a specified panel by selecting the corresponding item in the navigation list.
        /// </summary>
        /// <param name="window">The window containing the navigation list.</param>
        /// <param name="panelName">The name of the panel to navigate to.</param>
        public static void NavigateToPanel(Window window, string panelName)
        {
            var navList = window.FindControl<ListBox>("NavList");
            if (navList?.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == panelName) is
                { } item)
                navList.SelectedItem = item;
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }
}