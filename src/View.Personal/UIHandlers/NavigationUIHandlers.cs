namespace View.Personal.UIHandlers
{
    using Avalonia;
    using System;
    using System.Linq;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Helpers;
    using LiteGraph;

    /// <summary>
    /// Provides event handlers and utility methods for managing navigation in the user interface.
    /// </summary>
    public static class NavigationUIHandlers
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

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
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem selectedItem)
            {
                var selectedContent = selectedItem.Content?.ToString();

                window.FindControl<StackPanel>("DashboardPanel").IsVisible = false;
                window.FindControl<StackPanel>("SettingsPanel").IsVisible = false;
                window.FindControl<StackPanel>("MyFilesPanel").IsVisible = false;
                window.FindControl<StackPanel>("ChatPanel").IsVisible = false;
                window.FindControl<StackPanel>("ConsolePanel").IsVisible = false;
                window.FindControl<TextBlock>("WorkspaceText").IsVisible = false;

                switch (selectedContent)
                {
                    case "Dashboard":
                        window.FindControl<StackPanel>("DashboardPanel").IsVisible = true;
                        break;
                    case "Settings":
                        window.FindControl<StackPanel>("SettingsPanel").IsVisible = true;
                        var comboBox = window.FindControl<ComboBox>("NavModelProviderComboBox");
                        var currentProvider = (comboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                        if (!string.IsNullOrEmpty(currentProvider))
                            MainWindowUIHandlers.UpdateSettingsVisibility(window, currentProvider);
                        break;
                    case "My Files":
                        window.FindControl<StackPanel>("MyFilesPanel").IsVisible = true;
                        var filesDataGrid = window.FindControl<DataGrid>("FilesDataGrid");
                        if (filesDataGrid != null)
                        {
                            var uniqueFiles = MainWindowHelpers.GetDocumentNodes(liteGraph, tenantGuid, graphGuid);
                            filesDataGrid.ItemsSource = uniqueFiles.Any() ? uniqueFiles : null;
                            Console.WriteLine($"[INFO] Loaded {uniqueFiles.Count()} unique files into MyFilesPanel.");
                        }

                        break;
                    case "Chat":
                        window.FindControl<StackPanel>("ChatPanel").IsVisible = true;
                        break;
                    case "Console":
                        window.FindControl<StackPanel>("ConsolePanel").IsVisible = true;
                        break;
                    default:
                        window.FindControl<TextBlock>("WorkspaceText").IsVisible = true;
                        break;
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
                var selectedProvider = selectedItem.Content.ToString();
                Console.WriteLine($"[INFO] ModelProvider_SelectionChanged: {selectedProvider}");

                var app = (App)Application.Current;
                app.SaveSelectedProvider(selectedProvider);
                MainWindowUIHandlers.UpdateProviderSettings(window, selectedProvider);
                MainWindowUIHandlers.UpdateSettingsVisibility(window, selectedProvider);
            }
        }

        /// <summary>
        /// Handles the click event to navigate to the settings panel in the UI.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">The routed event arguments.</param>
        /// <param name="window">The window containing the navigation panels.</param>
        public static void NavigateToSettings_Click(object sender, RoutedEventArgs e, Window window)
        {
            NavigateToPanel(window, "Settings");
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
            if (navList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == panelName) is { } item)
                navList.SelectedItem = item;
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
    }
}