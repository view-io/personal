// View.Personal.UIHandlers/MainWindowUIHandlers.cs

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

namespace View.Personal.UIHandlers
{
    using System;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Interactivity;
    using Helpers;
    using MsBox.Avalonia.Enums;
    using Services;
    using LiteGraph;
    using DocumentAtom.TypeDetection;

    public static class MainWindowUIHandlers
    {
        public static void MainWindow_Opened(Window window)
        {
            Console.WriteLine("[INFO] MainWindow opened. Loading saved settings...");
            SettingsHelper.LoadSavedSettings(window);
            UpdateSettingsVisibility(window, "View");
            Console.WriteLine("[INFO] Finished MainWindow_Opened.");
            var consoleBox = window.FindControl<TextBox>("ConsoleOutputTextBox");
            if (consoleBox != null)
                Console.SetOut(new AvaloniaConsoleWriter(consoleBox));
        }

        public static async void SaveSettings_Click(object sender, RoutedEventArgs e, Window window)
        {
            Console.WriteLine("[INFO] SaveSettings_Click triggered.");
            var app = (App)Application.Current;
            var selectedProvider =
                (window.FindControl<ComboBox>("NavModelProviderComboBox").SelectedItem as ComboBoxItem)
                ?.Content.ToString();

            var settings = SettingsHelper.ExtractSettingsFromUI(window, selectedProvider);
            if (settings != null)
            {
                app.UpdateProviderSettings(settings);
                app.SaveSelectedProvider(selectedProvider);

                Console.WriteLine($"[INFO] {selectedProvider} settings saved successfully.");
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Settings Saved", $"{selectedProvider} settings saved successfully!",
                        ButtonEnum.Ok, Icon.Success)
                    .ShowAsync();
                SettingsHelper.LoadSavedSettings(window);
            }
            else
            {
                Console.WriteLine("[WARN] No settings were created because selectedProvider was null or invalid.");
            }
        }

        public static async void DeleteFile_Click(object sender, RoutedEventArgs e, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            Console.WriteLine("[INFO] DeleteFile_Click triggered.");
            await FileDeleter.DeleteFile_ClickAsync(sender, e, liteGraph, tenantGuid, graphGuid, window);
        }

        public static async void IngestFile_Click(object sender, RoutedEventArgs e, TypeDetector typeDetector,
            LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            Console.WriteLine("[INFO] IngestFile_Click triggered.");
            await FileIngester.IngestFile_ClickAsync(sender, e, typeDetector, liteGraph, tenantGuid, graphGuid, window);
        }

        public static void ExportGraph_Click(object sender, RoutedEventArgs e, LiteGraphClient liteGraph,
            Guid tenantGuid, Guid graphGuid, Window window)
        {
            Console.WriteLine("[INFO] ExportGraph_Click triggered.");
            GraphExporter.ExportGraph_Click(sender, e, liteGraph, tenantGuid, graphGuid, window);
        }

        public static async Task BrowseAndUpdateTextBoxAsync(Window window, string textBoxName,
            Func<Window, Task<string>> browseFunc)
        {
            Console.WriteLine($"[INFO] Browse triggered for {textBoxName}.");
            var textBox = window.FindControl<TextBox>(textBoxName);
            if (textBox == null) return;

            var filePath = await browseFunc(window);
            if (!string.IsNullOrEmpty(filePath))
            {
                textBox.Text = filePath;
                Console.WriteLine($"[INFO] User selected path: {filePath}");
            }
        }

        public static async void BrowseButton_Click(object sender, RoutedEventArgs e, Window window,
            FileBrowserService fileBrowserService)
        {
            await BrowseAndUpdateTextBoxAsync(window, "ExportFilePathTextBox",
                w => fileBrowserService.BrowseForExportLocation(w));
        }

        public static async void IngestBrowseButton_Click(object sender, RoutedEventArgs e, Window window,
            FileBrowserService fileBrowserService)
        {
            await BrowseAndUpdateTextBoxAsync(window, "FilePathTextBox",
                w => fileBrowserService.BrowseForFileToIngest(w));
        }

        public static void UpdateButtonEnabledOnTextChange(object sender, AvaloniaPropertyChangedEventArgs e,
            Window window, string buttonName)
        {
            if (e.Property.Name == "Text" && sender is TextBox textBox)
            {
                var button = window.FindControl<Button>(buttonName);
                if (button != null)
                    button.IsEnabled = !string.IsNullOrWhiteSpace(textBox.Text);
            }
        }

        public static void FilePathTextBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e,
            Window window)
        {
            UpdateButtonEnabledOnTextChange(sender, e, window, "IngestButton");
        }

        public static void ExportFilePathTextBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e,
            Window window)
        {
            UpdateButtonEnabledOnTextChange(sender, e, window, "ExportButton");
        }

        public static void UpdateSettingsVisibility(Window window, string selectedProvider)
        {
            Console.WriteLine($"[INFO] Updating settings visibility for provider: {selectedProvider}");
            MainWindowHelpers.UpdateSettingsVisibility(
                window.FindControl<Control>("OpenAISettings"),
                window.FindControl<Control>("AnthropicSettings"),
                window.FindControl<Control>("ViewSettings"),
                window.FindControl<Control>("OllamaSettings"),
                selectedProvider);
        }

        public static void UpdateProviderSettings(Window window, string selectedProvider)
        {
            var app = (App)Application.Current;
            var settings = SettingsHelper.ExtractSettingsFromUI(window, selectedProvider);

            if (settings != null)
            {
                app.UpdateProviderSettings(settings);
                Console.WriteLine($"[INFO] {selectedProvider} settings updated due to provider change.");
            }
        }
    }
}