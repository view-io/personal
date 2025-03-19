namespace View.Personal.UIHandlers
{
    using Avalonia;
    using System;
    using System.Linq;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Helpers;
    using LiteGraph;

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

        public static void NavigateToSettings_Click(object sender, RoutedEventArgs e, Window window)
        {
            NavigateToPanel(window, "Settings");
        }

        public static void NavigateToMyFiles_Click(object sender, RoutedEventArgs e, Window window)
        {
            NavigateToPanel(window, "My Files");
        }

        public static void NavigateToChat_Click(object sender, RoutedEventArgs e, Window window)
        {
            NavigateToPanel(window, "Chat");
        }

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