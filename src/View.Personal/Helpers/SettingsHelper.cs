#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
namespace View.Personal.Helpers
{
    using System;
    using System.Collections.Generic;
    using Avalonia.Controls;
    using Classes;
    using System.Globalization;
    using System.Linq;
    using Avalonia;

    public static class SettingsHelper
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Extracts provider-specific settings from UI controls in the specified window based on the selected provider
        /// <param name="window">The window containing the UI controls with settings data</param>
        /// <param name="selectedProvider">The string indicating the currently selected provider (e.g., "OpenAI", "Anthropic", "View", "Ollama")</param>
        /// Returns:
        /// A CompletionProviderSettings object populated with values from the UI, or null if the provider is not supported
        /// </summary>
        public static CompletionProviderSettings ExtractSettingsFromUI(Window window, string selectedProvider)
        {
            var settingsMap = new Dictionary<string, Func<CompletionProviderSettings>>
            {
                ["OpenAI"] = () => new CompletionProviderSettings(CompletionProviderTypeEnum.OpenAI)
                {
                    OpenAICompletionApiKey = GetTextBoxValue(window, "OpenAIKey"),
                    OpenAIEmbeddingModel = GetTextBoxValue(window, "OpenAIEmbeddingModel"),
                    OpenAICompletionModel = GetTextBoxValue(window, "OpenAICompletionModel"),
                    OpenAIMaxTokens = ParseIntOrDefault(window, "OpenAIMaxTokens", 300),
                    OpenAITemperature = GetNumericUpDownValueOrNull(window, "OpenAITemperature"),
                    OpenAIReasoningEffort = GetReasoningEffortValue(window, "OpenAIReasoningEffort")
                },
                ["Anthropic"] = () => new CompletionProviderSettings(CompletionProviderTypeEnum.Anthropic)
                {
                    AnthropicCompletionModel = GetTextBoxValue(window, "AnthropicCompletionModel"),
                    AnthropicApiKey = GetTextBoxValue(window, "AnthropicApiKey"),
                    VoyageApiKey = GetTextBoxValue(window, "VoyageApiKey"),
                    VoyageEmbeddingModel = GetTextBoxValue(window, "VoyageEmbeddingModel")
                },
                ["View"] = () => new CompletionProviderSettings(CompletionProviderTypeEnum.View)
                {
                    ViewEmbeddingsGenerator = GetComboBoxValue(window, "ViewEmbeddingsGenerator"),
                    ViewApiKey = GetTextBoxValue(window, "ViewApiKey"),
                    ViewEndpoint = GetTextBoxValue(window, "ViewEndpoint"),
                    ViewAccessKey = GetTextBoxValue(window, "ViewAccessKey"),
                    ViewEmbeddingsGeneratorUrl = GetTextBoxValue(window, "ViewEmbeddingsGeneratorUrl"),
                    ViewModel = GetTextBoxValue(window, "ViewModel"),
                    ViewCompletionApiKey = GetTextBoxValue(window, "ViewCompletionApiKey"),
                    ViewCompletionProvider = GetTextBoxValue(window, "ViewCompletionProvider"),
                    ViewCompletionModel = GetTextBoxValue(window, "ViewCompletionModel"),
                    ViewCompletionPort = ParseIntOrDefault(window, "ViewCompletionPort", 0),
                    ViewTemperature = GetNumericUpDownFloatValueOrNull(window, "ViewTemperature"),
                    ViewTopP = GetNumericUpDownFloatValueOrNull(window, "ViewTopP"),
                    ViewMaxTokens = GetIntUpDownValue(window, "ViewMaxTokens")
                },
                ["Ollama"] = () => new CompletionProviderSettings(CompletionProviderTypeEnum.Ollama)
                {
                    OllamaModel = GetTextBoxValue(window, "OllamaModel"),
                    OllamaCompletionModel = GetTextBoxValue(window, "OllamaCompletionModel"),
                    OllamaTemperature = ParseDoubleOrDefault(window, "OllamaTemperature", 0.7),
                    OllamaTopP = ParseDoubleOrDefault(window, "OllamaTopP", 1.0),
                    OllamaMaxTokens = ParseIntOrDefault(window, "OllamaMaxTokens", 150)
                }
            };

            return settingsMap.TryGetValue(selectedProvider, out var factory) ? factory() : null;
        }

        /// <summary>
        /// Loads saved provider settings from the application and populates the corresponding UI controls
        /// <param name="window">The window</param>
        /// Returns:
        /// None; updates UI controls with loaded settings directly
        /// </summary>
        public static void LoadSavedSettings(Window window)
        {
            try
            {
                Console.WriteLine("[INFO] Loading settings from app.AppSettings...");
                var app = (App)Application.Current;

                var view = app.GetProviderSettings(CompletionProviderTypeEnum.View);
                var embeddingsGeneratorComboBox = window.FindControl<ComboBox>("ViewEmbeddingsGenerator");
                if (!string.IsNullOrEmpty(view.ViewEmbeddingsGenerator))
                {
                    var selectedItem = embeddingsGeneratorComboBox.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content.ToString() == view.ViewEmbeddingsGenerator);

                    embeddingsGeneratorComboBox.SelectedItem = selectedItem ?? embeddingsGeneratorComboBox.Items[0];
                }
                else
                {
                    embeddingsGeneratorComboBox.SelectedIndex = 0;
                }

                window.FindControl<TextBox>("ViewApiKey").Text = view.ViewApiKey ?? string.Empty;
                window.FindControl<TextBox>("ViewEndpoint").Text = view.ViewEndpoint ?? string.Empty;
                window.FindControl<TextBox>("ViewAccessKey").Text = view.ViewAccessKey ?? string.Empty;
                window.FindControl<TextBox>("ViewEmbeddingsGeneratorUrl").Text =
                    view.ViewEmbeddingsGeneratorUrl ?? string.Empty;
                window.FindControl<TextBox>("ViewModel").Text = view.ViewModel ?? string.Empty;
                window.FindControl<TextBox>("ViewCompletionApiKey").Text = view.ViewCompletionApiKey ?? string.Empty;
                window.FindControl<TextBox>("ViewCompletionProvider").Text =
                    view.ViewCompletionProvider ?? string.Empty;
                window.FindControl<TextBox>("ViewCompletionModel").Text = view.ViewCompletionModel ?? string.Empty;
                window.FindControl<TextBox>("ViewCompletionPort").Text = view.ViewCompletionPort.ToString();
                var viewTemperatureControl = window.FindControl<NumericUpDown>("ViewTemperature");
                if (viewTemperatureControl != null) viewTemperatureControl.Value = (decimal)view.ViewTemperature;
                var viewTopPControl = window.FindControl<NumericUpDown>("ViewTopP");
                if (viewTopPControl != null) viewTopPControl.Value = (decimal)view.ViewTopP;
                var maxTokensControl = window.FindControl<NumericUpDown>("ViewMaxTokens");
                if (maxTokensControl != null) maxTokensControl.Value = view.ViewMaxTokens;


                var openAI = app.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
                window.FindControl<TextBox>("OpenAIKey").Text = openAI.OpenAICompletionApiKey ?? string.Empty;
                window.FindControl<TextBox>("OpenAIEmbeddingModel").Text = openAI.OpenAIEmbeddingModel ?? string.Empty;
                window.FindControl<TextBox>("OpenAICompletionModel").Text =
                    openAI.OpenAICompletionModel ?? string.Empty;
                window.FindControl<TextBox>("OpenAIMaxTokens").Text = openAI.OpenAIMaxTokens.ToString();
                var temperatureControl = window.FindControl<NumericUpDown>("OpenAITemperature");
                if (openAI.OpenAITemperature.HasValue)
                    temperatureControl.Value = (decimal)openAI.OpenAITemperature.Value;
                else
                    temperatureControl.Value = null;
                var reasoningEffortControl = window.FindControl<ComboBox>("OpenAIReasoningEffort");
                var effortLevel = openAI.GetReasoningEffortLevel();

                if (effortLevel == null)
                {
                    reasoningEffortControl.SelectedIndex = 0;
                }
                else
                {
                    var effortName = effortLevel.ToString();
                    var item = reasoningEffortControl.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(
                            i => i.Content.ToString().Equals(effortName, StringComparison.OrdinalIgnoreCase));

                    if (item != null)
                        reasoningEffortControl.SelectedItem = item;
                    else
                        reasoningEffortControl.SelectedIndex = 0;
                }


                var anthropic = app.GetProviderSettings(CompletionProviderTypeEnum.Anthropic);
                window.FindControl<TextBox>("AnthropicCompletionModel").Text =
                    anthropic.AnthropicCompletionModel ?? string.Empty;
                window.FindControl<TextBox>("AnthropicApiKey").Text = anthropic.AnthropicApiKey ?? string.Empty;
                window.FindControl<TextBox>("VoyageApiKey").Text = anthropic.VoyageApiKey ?? string.Empty;
                window.FindControl<TextBox>("VoyageEmbeddingModel").Text =
                    anthropic.VoyageEmbeddingModel ?? string.Empty;

                var ollama = app.GetProviderSettings(CompletionProviderTypeEnum.Ollama);
                window.FindControl<TextBox>("OllamaModel").Text = ollama.OllamaModel ?? string.Empty;
                window.FindControl<TextBox>("OllamaCompletionModel").Text =
                    ollama.OllamaCompletionModel ?? string.Empty;
                window.FindControl<TextBox>("OllamaTemperature").Text =
                    ollama.OllamaTemperature.ToString(CultureInfo.InvariantCulture);
                window.FindControl<TextBox>("OllamaTopP").Text =
                    ollama.OllamaTopP.ToString(CultureInfo.InvariantCulture);
                window.FindControl<TextBox>("OllamaMaxTokens").Text = ollama.OllamaMaxTokens.ToString();

                var comboBox = window.FindControl<ComboBox>("NavModelProviderComboBox");
                if (!string.IsNullOrEmpty(app.AppSettings.SelectedProvider))
                {
                    var selectedItem = comboBox.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content.ToString() == app.AppSettings.SelectedProvider);
                    comboBox.SelectedItem = selectedItem ?? comboBox.Items[0];
                }
                else
                {
                    comboBox.SelectedIndex = 0;
                }

                Console.WriteLine("[INFO] Finished loading settings.");
                MainWindowHelpers.UpdateSettingsVisibility(
                    window.FindControl<Control>("OpenAISettings"),
                    window.FindControl<Control>("AnthropicSettings"),
                    window.FindControl<Control>("ViewSettings"),
                    window.FindControl<Control>("OllamaSettings"),
                    app.AppSettings.SelectedProvider ?? "View");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Retrieves the text value from a TextBox control in the specified window, returning an empty string if not found
        /// <param name="window">The window containing the TextBox control</param>
        /// <param name="controlName">The name of the TextBox control to query</param>
        /// Returns:
        /// The text content of the TextBox, or an empty string if the control is not found or has no text
        /// </summary>
        private static string GetTextBoxValue(Window window, string controlName)
        {
            return window.FindControl<TextBox>(controlName)?.Text ?? string.Empty;
        }

        private static string GetComboBoxValue(Window window, string controlName)
        {
            var comboBox = window.FindControl<ComboBox>(controlName);
            if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
                return selectedItem.Content?.ToString() ?? string.Empty;
            return string.Empty;
        }

        /// <summary>
        /// Parses the text value from a TextBox control into an integer, returning a default value if parsing fails
        /// <param name="window">The window containing the TextBox control</param>
        /// <param name="controlName">The name of the TextBox control to query</param>
        /// <param name="defaultValue">The default integer value to return if parsing is unsuccessful</param>
        /// Returns:
        /// The parsed integer value from the TextBox, or the defaultValue if the text is not a valid integer
        /// </summary>
        private static int ParseIntOrDefault(Window window, string controlName, int defaultValue)
        {
            return int.TryParse(GetTextBoxValue(window, controlName), out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Parses the text value from a TextBox control into a double, returning a default value if parsing fails
        /// <param name="window">The window containing the TextBox control</param>
        /// <param name="controlName">The name of the TextBox control to query</param>
        /// <param name="defaultValue">The default double value to return if parsing is unsuccessful</param>
        /// Returns:
        /// The parsed double value from the TextBox, or the defaultValue if the text is not a valid double
        /// </summary>
        private static double ParseDoubleOrDefault(Window window, string controlName, double defaultValue)
        {
            return double.TryParse(GetTextBoxValue(window, controlName), out var value) ? value : defaultValue;
        }

        private static float? ParseFloatOrNullable(Window window, string controlName)
        {
            var text = GetTextBoxValue(window, controlName);
            if (string.IsNullOrWhiteSpace(text)) return null;

            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) return value;

            return null;
        }

        private static float GetNumericUpDownFloatValueOrNull(Window window, string controlName)
        {
            var control = window.FindControl<NumericUpDown>(controlName);
            if (control.Value.HasValue)
                return (float)control.Value.Value;
            return 0.95f;
        }

        private static double? GetNumericUpDownValueOrNull(Window window, string controlName)
        {
            var control = window.FindControl<NumericUpDown>(controlName);
            if (control != null && control.Value.HasValue) return (double)control.Value.Value;
            return null;
        }

        private static int GetIntUpDownValue(Window window, string controlName)
        {
            var control = window.FindControl<NumericUpDown>(controlName);
            if (control.Value.HasValue)
                return (int)control.Value.Value;
            return 1000;
        }

        private static int? ParseIntOrNullable(Window window, string controlName)
        {
            var text = GetTextBoxValue(window, controlName);
            if (string.IsNullOrWhiteSpace(text)) return null;

            if (int.TryParse(text, out var value)) return value;

            return null;
        }

        private static string? GetReasoningEffortValue(Window window, string controlName)
        {
            var comboBox = window.FindControl<ComboBox>(controlName);
            if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                var content = selectedItem.Content?.ToString() ?? string.Empty;

                // Return null for "Default" to use OpenAI's default (medium)
                if (content == "Default")
                    return null;

                // Try to parse as enum and convert to lowercase string
                if (Enum.TryParse<OpenAIReasoningEffortEnum>(content, true, out var level))
                    return level.ToString().ToLowerInvariant();
            }

            return null;
        }

        #endregion
    }
}