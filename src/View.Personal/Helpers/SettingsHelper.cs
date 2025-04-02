// namespace View.Personal.Helpers
// {
//     using System;
//     using System.Collections.Generic;
//     using Avalonia.Controls;
//     using Classes;
//     using System.Globalization;
//     using System.Linq;
//     using Avalonia;
//
//     /// <summary>
//     /// Provides helper methods for managing settings extraction and loading within the application UI.
//     /// </summary>
//     public static class SettingsHelper
//     {
// #pragma warning disable CS8603 // Possible null reference return.
// #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
//
//
//         #region Public-Members
//
//         #endregion
//
//         #region Private-Members
//
//         #endregion
//
//         #region Public-Methods
//
//         /// <summary>
//         /// Extracts provider-specific settings from UI controls in the specified window based on the selected provider
//         /// <param name="window">The window containing the UI controls with settings data</param>
//         /// <param name="selectedProvider">The string indicating the currently selected provider (e.g., "OpenAI", "Anthropic", "View", "Ollama")</param>
//         /// Returns:
//         /// A CompletionProviderSettings object populated with values from the UI, or null if the provider is not supported
//         /// </summary>
//         public static CompletionProviderSettings ExtractSettingsFromUI(Window window, string selectedProvider)
//         {
//             var settingsMap = new Dictionary<string, Func<CompletionProviderSettings>>
//             {
//                 ["OpenAI"] = () => new CompletionProviderSettings(CompletionProviderTypeEnum.OpenAI)
//                 {
//                     OpenAICompletionApiKey = GetTextBoxValue(window, "OpenAIKey"),
//                     OpenAIEmbeddingModel = GetTextBoxValue(window, "OpenAIEmbeddingModel"),
//                     OpenAICompletionModel = GetTextBoxValue(window, "OpenAICompletionModel"),
//                     OpenAIMaxTokens = ParseIntOrDefault(window, "OpenAIMaxTokens", 300),
//                     OpenAITemperature = GetNumericUpDownValueOrNull(window, "OpenAITemperature"),
//                     OpenAIReasoningEffort = GetReasoningEffortValue(window, "OpenAIReasoningEffort")
//                 },
//                 ["Anthropic"] = () => new CompletionProviderSettings(CompletionProviderTypeEnum.Anthropic)
//                 {
//                     AnthropicCompletionModel = GetTextBoxValue(window, "AnthropicCompletionModel"),
//                     AnthropicApiKey = GetTextBoxValue(window, "AnthropicApiKey"),
//                     VoyageApiKey = GetTextBoxValue(window, "VoyageApiKey"),
//                     VoyageEmbeddingModel = GetTextBoxValue(window, "VoyageEmbeddingModel")
//                 },
//                 ["View"] = () => new CompletionProviderSettings(CompletionProviderTypeEnum.View)
//                 {
//                     ViewEmbeddingsGenerator = GetComboBoxValue(window, "ViewEmbeddingsGenerator"),
//                     ViewApiKey = GetTextBoxValue(window, "ViewApiKey"),
//                     ViewEndpoint = GetTextBoxValue(window, "ViewEndpoint"),
//                     ViewAccessKey = GetTextBoxValue(window, "ViewAccessKey"),
//                     ViewEmbeddingsGeneratorUrl = GetTextBoxValue(window, "ViewEmbeddingsGeneratorUrl"),
//                     ViewModel = GetTextBoxValue(window, "ViewModel"),
//                     ViewCompletionApiKey = GetTextBoxValue(window, "ViewCompletionApiKey"),
//                     ViewCompletionProvider = GetTextBoxValue(window, "ViewCompletionProvider"),
//                     ViewCompletionModel = GetTextBoxValue(window, "ViewCompletionModel"),
//                     ViewCompletionPort = ParseIntOrDefault(window, "ViewCompletionPort", 0),
//                     ViewTemperature = GetNumericUpDownFloatValueOrNull(window, "ViewTemperature"),
//                     ViewTopP = GetNumericUpDownFloatValueOrNull(window, "ViewTopP"),
//                     ViewMaxTokens = GetIntUpDownValue(window, "ViewMaxTokens")
//                 },
//                 ["Ollama"] = () => new CompletionProviderSettings(CompletionProviderTypeEnum.Ollama)
//                 {
//                     OllamaModel = GetTextBoxValue(window, "OllamaModel"),
//                     OllamaCompletionModel = GetTextBoxValue(window, "OllamaCompletionModel"),
//                     OllamaTemperature = ParseDoubleOrDefault(window, "OllamaTemperature", 0.7),
//                     OllamaTopP = ParseDoubleOrDefault(window, "OllamaTopP", 1.0),
//                     OllamaMaxTokens = ParseIntOrDefault(window, "OllamaMaxTokens", 150)
//                 }
//             };
//
//             return settingsMap.TryGetValue(selectedProvider, out var factory) ? factory() : null;
//         }
//
//         /// <summary>
//         /// Loads saved provider settings from the application and populates the corresponding UI controls
//         /// <param name="window">The window</param>
//         /// Returns:
//         /// None; updates UI controls with loaded settings directly
//         /// </summary>
//         public static void LoadSavedSettings(Window window)
//         {
//             try
//             {
//                 Console.WriteLine("[INFO] Loading settings from app.AppSettings...");
//                 var app = (App)Application.Current;
//
//                 var view = app?.GetProviderSettings(CompletionProviderTypeEnum.View);
//                 var embeddingsGeneratorComboBox = window.FindControl<ComboBox>("ViewEmbeddingsGenerator");
//                 if (!string.IsNullOrEmpty(view?.ViewEmbeddingsGenerator))
//                 {
//                     var selectedItem = embeddingsGeneratorComboBox?.Items
//                         .OfType<ComboBoxItem>()
//                         .FirstOrDefault(item => item.Content?.ToString() == view.ViewEmbeddingsGenerator);
//                     if (embeddingsGeneratorComboBox != null)
//                         embeddingsGeneratorComboBox.SelectedItem = selectedItem ?? embeddingsGeneratorComboBox.Items[0];
//                 }
//                 else
//                 {
//                     if (embeddingsGeneratorComboBox != null)
//                         embeddingsGeneratorComboBox.SelectedIndex = 0;
//                 }
//
//                 var viewApiKeyTextBox = window.FindControl<TextBox>("ViewApiKey");
//                 if (viewApiKeyTextBox != null) viewApiKeyTextBox.Text = view?.ViewApiKey ?? string.Empty;
//
//                 var viewEndpointTextBox = window.FindControl<TextBox>("ViewEndpoint");
//                 if (viewEndpointTextBox != null) viewEndpointTextBox.Text = view?.ViewEndpoint ?? string.Empty;
//
//                 var viewAccessKeyTextBox = window.FindControl<TextBox>("ViewAccessKey");
//                 if (viewAccessKeyTextBox != null) viewAccessKeyTextBox.Text = view?.ViewAccessKey ?? string.Empty;
//
//                 var viewEmbeddingsGeneratorUrlTextBox = window.FindControl<TextBox>("ViewEmbeddingsGeneratorUrl");
//                 if (viewEmbeddingsGeneratorUrlTextBox != null)
//                     viewEmbeddingsGeneratorUrlTextBox.Text = view?.ViewEmbeddingsGeneratorUrl ?? string.Empty;
//
//                 var viewModelTextBox = window.FindControl<TextBox>("ViewModel");
//                 if (viewModelTextBox != null) viewModelTextBox.Text = view?.ViewModel ?? string.Empty;
//
//                 var viewCompletionApiKeyTextBox = window.FindControl<TextBox>("ViewCompletionApiKey");
//                 if (viewCompletionApiKeyTextBox != null)
//                     viewCompletionApiKeyTextBox.Text = view?.ViewCompletionApiKey ?? string.Empty;
//
//                 var viewCompletionProviderTextBox = window.FindControl<TextBox>("ViewCompletionProvider");
//                 if (viewCompletionProviderTextBox != null)
//                     viewCompletionProviderTextBox.Text = view?.ViewCompletionProvider ?? string.Empty;
//
//                 var viewCompletionModelTextBox = window.FindControl<TextBox>("ViewCompletionModel");
//                 if (viewCompletionModelTextBox != null)
//                     viewCompletionModelTextBox.Text = view?.ViewCompletionModel ?? string.Empty;
//
//                 var viewCompletionPortTextBox = window.FindControl<TextBox>("ViewCompletionPort");
//                 if (viewCompletionPortTextBox != null)
//                     viewCompletionPortTextBox.Text = view?.ViewCompletionPort.ToString();
//
//                 var viewTemperatureControl = window.FindControl<NumericUpDown>("ViewTemperature");
//                 if (viewTemperatureControl != null && view != null)
//                     viewTemperatureControl.Value = (decimal)view.ViewTemperature;
//
//                 var viewTopPControl = window.FindControl<NumericUpDown>("ViewTopP");
//                 if (viewTopPControl != null && view != null) viewTopPControl.Value = (decimal)view.ViewTopP;
//
//                 var maxTokensControl = window.FindControl<NumericUpDown>("ViewMaxTokens");
//                 if (maxTokensControl != null) maxTokensControl.Value = view?.ViewMaxTokens;
//
//                 var openAI = app?.GetProviderSettings(CompletionProviderTypeEnum.OpenAI);
//
//                 if (openAI != null)
//                 {
//                     var openAIKeyTextBox = window.FindControl<TextBox>("OpenAIKey");
//                     if (openAIKeyTextBox != null) openAIKeyTextBox.Text = openAI.OpenAICompletionApiKey;
//
//                     var openAIEmbeddingModelTextBox = window.FindControl<TextBox>("OpenAIEmbeddingModel");
//                     if (openAIEmbeddingModelTextBox != null)
//                         openAIEmbeddingModelTextBox.Text = openAI.OpenAIEmbeddingModel;
//
//                     var openAICompletionModelTextBox = window.FindControl<TextBox>("OpenAICompletionModel");
//                     if (openAICompletionModelTextBox != null)
//                         openAICompletionModelTextBox.Text = openAI.OpenAICompletionModel;
//
//                     var openAIMaxTokensTextBox = window.FindControl<TextBox>("OpenAIMaxTokens");
//                     if (openAIMaxTokensTextBox != null) openAIMaxTokensTextBox.Text = openAI.OpenAIMaxTokens.ToString();
//                 }
//
//                 var temperatureControl = window.FindControl<NumericUpDown>("OpenAITemperature");
//                 if (temperatureControl != null)
//                 {
//                     if (openAI != null && openAI.OpenAITemperature.HasValue)
//                         temperatureControl.Value = (decimal)openAI.OpenAITemperature.Value;
//                     else
//                         temperatureControl.Value = null;
//                 }
//
//                 var reasoningEffortControl = window.FindControl<ComboBox>("OpenAIReasoningEffort");
//                 if (reasoningEffortControl != null)
//                 {
//                     var effortLevel = openAI?.GetReasoningEffortLevel();
//                     if (effortLevel == null)
//                     {
//                         reasoningEffortControl.SelectedIndex = 0;
//                     }
//                     else
//                     {
//                         var effortName = effortLevel.ToString();
//                         var item = reasoningEffortControl.Items
//                             .OfType<ComboBoxItem>()
//                             .FirstOrDefault(i =>
//                                 i.Content != null && i.Content.ToString()
//                                     ?.Equals(effortName, StringComparison.OrdinalIgnoreCase) == true);
//                         if (item != null)
//                             reasoningEffortControl.SelectedItem = item;
//                         else
//                             reasoningEffortControl.SelectedIndex = 0;
//                     }
//                 }
//
//                 var anthropic = app?.GetProviderSettings(CompletionProviderTypeEnum.Anthropic);
//
//                 var anthropicCompletionModelTextBox = window.FindControl<TextBox>("AnthropicCompletionModel");
//                 if (anthropicCompletionModelTextBox != null)
//                     anthropicCompletionModelTextBox.Text = anthropic?.AnthropicCompletionModel ?? string.Empty;
//
//                 var anthropicApiKeyTextBox = window.FindControl<TextBox>("AnthropicApiKey");
//                 if (anthropicApiKeyTextBox != null)
//                     anthropicApiKeyTextBox.Text = anthropic?.AnthropicApiKey ?? string.Empty;
//
//                 var voyageApiKeyTextBox = window.FindControl<TextBox>("VoyageApiKey");
//                 if (voyageApiKeyTextBox != null) voyageApiKeyTextBox.Text = anthropic?.VoyageApiKey ?? string.Empty;
//
//                 var voyageEmbeddingModelTextBox = window.FindControl<TextBox>("VoyageEmbeddingModel");
//                 if (voyageEmbeddingModelTextBox != null)
//                     voyageEmbeddingModelTextBox.Text = anthropic?.VoyageEmbeddingModel ?? string.Empty;
//
//                 var ollama = app?.GetProviderSettings(CompletionProviderTypeEnum.Ollama);
//
//                 var ollamaModelTextBox = window.FindControl<TextBox>("OllamaModel");
//                 if (ollamaModelTextBox != null) ollamaModelTextBox.Text = ollama?.OllamaModel ?? string.Empty;
//
//                 var ollamaCompletionModelTextBox = window.FindControl<TextBox>("OllamaCompletionModel");
//                 if (ollamaCompletionModelTextBox != null)
//                     ollamaCompletionModelTextBox.Text = ollama?.OllamaCompletionModel ?? string.Empty;
//
//                 var ollamaTemperatureTextBox = window.FindControl<TextBox>("OllamaTemperature");
//                 if (ollamaTemperatureTextBox != null)
//                     ollamaTemperatureTextBox.Text = ollama?.OllamaTemperature.ToString(CultureInfo.InvariantCulture);
//
//                 var ollamaTopPTextBox = window.FindControl<TextBox>("OllamaTopP");
//                 if (ollamaTopPTextBox != null)
//                     ollamaTopPTextBox.Text = ollama?.OllamaTopP.ToString(CultureInfo.InvariantCulture);
//
//                 var ollamaMaxTokensTextBox = window.FindControl<TextBox>("OllamaMaxTokens");
//                 if (ollamaMaxTokensTextBox != null) ollamaMaxTokensTextBox.Text = ollama?.OllamaMaxTokens.ToString();
//
//                 var comboBox = window.FindControl<ComboBox>("NavModelProviderComboBox");
//                 if (comboBox != null)
//                 {
//                     if (!string.IsNullOrEmpty(app?.AppSettings.SelectedProvider))
//                     {
//                         var selectedItem = comboBox.Items
//                             .OfType<ComboBoxItem>()
//                             .FirstOrDefault(item => item.Content?.ToString() == app.AppSettings.SelectedProvider);
//                         comboBox.SelectedItem = selectedItem ?? comboBox.Items[0];
//                     }
//                     else
//                     {
//                         comboBox.SelectedIndex = 0;
//                     }
//                 }
//
//                 Console.WriteLine("[INFO] Finished loading settings.");
//
//                 var openAISettings = window.FindControl<Control>("OpenAISettings");
//                 var anthropicSettings = window.FindControl<Control>("AnthropicSettings");
//                 var viewSettings = window.FindControl<Control>("ViewSettings");
//                 var ollamaSettings = window.FindControl<Control>("OllamaSettings");
//
//                 if (openAISettings != null && anthropicSettings != null && viewSettings != null &&
//                     ollamaSettings != null)
//                     MainWindowHelpers.UpdateSettingsVisibility(
//                         openAISettings,
//                         anthropicSettings,
//                         viewSettings,
//                         ollamaSettings,
//                         app?.AppSettings.SelectedProvider ?? "View");
//                 else
//                     Console.WriteLine("[ERROR] One or more settings controls are null.");
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(e);
//                 throw;
//             }
//         }
//
//         #endregion
//
//         #region Private-Methods
//
//         /// <summary>
//         /// Retrieves the text value from a TextBox control in the specified window, returning an empty string if not found
//         /// <param name="window">The window containing the TextBox control</param>
//         /// <param name="controlName">The name of the TextBox control to query</param>
//         /// Returns:
//         /// The text content of the TextBox, or an empty string if the control is not found or has no text
//         /// </summary>
//         private static string GetTextBoxValue(Window window, string controlName)
//         {
//             return window.FindControl<TextBox>(controlName)?.Text ?? string.Empty;
//         }
//
//         /// <summary>
//         /// Retrieves the selected value from a ComboBox control within a specified window.
//         /// </summary>
//         /// <param name="window">The window containing the ComboBox control.</param>
//         /// <param name="controlName">The name of the ComboBox control to retrieve the value from.</param>
//         /// <returns>The string content of the selected ComboBox item, or an empty string if no item is selected or the control is not found.</returns>
//         private static string GetComboBoxValue(Window window, string controlName)
//         {
//             var comboBox = window.FindControl<ComboBox>(controlName);
//             if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
//                 return selectedItem.Content?.ToString() ?? string.Empty;
//             return string.Empty;
//         }
//
//         /// <summary>
//         /// Parses the text value from a TextBox control into an integer, returning a default value if parsing fails
//         /// <param name="window">The window containing the TextBox control</param>
//         /// <param name="controlName">The name of the TextBox control to query</param>
//         /// <param name="defaultValue">The default integer value to return if parsing is unsuccessful</param>
//         /// Returns:
//         /// The parsed integer value from the TextBox, or the defaultValue if the text is not a valid integer
//         /// </summary>
//         private static int ParseIntOrDefault(Window window, string controlName, int defaultValue)
//         {
//             return int.TryParse(GetTextBoxValue(window, controlName), out var value) ? value : defaultValue;
//         }
//
//         /// <summary>
//         /// Parses the text value from a TextBox control into a double, returning a default value if parsing fails
//         /// <param name="window">The window containing the TextBox control</param>
//         /// <param name="controlName">The name of the TextBox control to query</param>
//         /// <param name="defaultValue">The default double value to return if parsing is unsuccessful</param>
//         /// Returns:
//         /// The parsed double value from the TextBox, or the defaultValue if the text is not a valid double
//         /// </summary>
//         private static double ParseDoubleOrDefault(Window window, string controlName, double defaultValue)
//         {
//             return double.TryParse(GetTextBoxValue(window, controlName), out var value) ? value : defaultValue;
//         }
//
//         /// <summary>
//         /// Retrieves the float value from a NumericUpDown control within a specified window, or a default value if none is set.
//         /// </summary>
//         /// <param name="window">The window containing the NumericUpDown control.</param>
//         /// <param name="controlName">The name of the NumericUpDown control to retrieve the value from.</param>
//         /// <returns>The float value of the NumericUpDown control if it has a value; otherwise, returns 0.95f as a default.</returns>
//         private static float GetNumericUpDownFloatValueOrNull(Window window, string controlName)
//         {
//             var control = window.FindControl<NumericUpDown>(controlName);
//             if (control != null && control.Value.HasValue)
//                 return (float)control.Value.Value;
//             return 0.95f;
//         }
//
//         /// <summary>
//         /// Retrieves the double value from a NumericUpDown control within a specified window, or null if no value is set or the control is not found.
//         /// </summary>
//         /// <param name="window">The window containing the NumericUpDown control.</param>
//         /// <param name="controlName">The name of the NumericUpDown control to retrieve the value from.</param>
//         /// <returns>The double value of the NumericUpDown control if it exists and has a value; otherwise, returns null.</returns>
//         private static double? GetNumericUpDownValueOrNull(Window window, string controlName)
//         {
//             var control = window.FindControl<NumericUpDown>(controlName);
//             if (control != null && control.Value.HasValue) return (double)control.Value.Value;
//             return null;
//         }
//
//         /// <summary>
//         /// Retrieves the integer value from a NumericUpDown control within a specified window, or a default value if none is set.
//         /// </summary>
//         /// <param name="window">The window containing the NumericUpDown control.</param>
//         /// <param name="controlName">The name of the NumericUpDown control to retrieve the value from.</param>
//         /// <returns>The integer value of the NumericUpDown control if it has a value; otherwise, returns 1000 as a default.</returns>
//         private static int GetIntUpDownValue(Window window, string controlName)
//         {
//             var control = window.FindControl<NumericUpDown>(controlName);
//             if (control != null && control.Value.HasValue)
//                 return (int)control.Value.Value;
//             return 1000;
//         }
//
//         /// <summary>
//         /// Retrieves the reasoning effort value from a ComboBox control within a specified window, converting it to a lowercase string or null for specific cases.
//         /// </summary>
//         /// <param name="window">The window containing the ComboBox control.</param>
//         /// <param name="controlName">The name of the ComboBox control to retrieve the value from.</param>
//         /// <returns>A lowercase string representation of the selected reasoning effort level, or null if the selection is "Default" or invalid.</returns>
//         private static string? GetReasoningEffortValue(Window window, string controlName)
//         {
//             var comboBox = window.FindControl<ComboBox>(controlName);
//             if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
//             {
//                 var content = selectedItem.Content?.ToString() ?? string.Empty;
//
//                 // Return null for "Default" to use OpenAI's default (medium)
//                 if (content == "Default")
//                     return null;
//
//                 // Try to parse as enum and convert to lowercase string
//                 if (Enum.TryParse<OpenAIReasoningEffortEnum>(content, true, out var level))
//                     return level.ToString().ToLowerInvariant();
//             }
//
//             return null;
//         }
//
//         #endregion
//
// #pragma warning restore CS8603 // Possible null reference return.
// #pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
//     }
// }

