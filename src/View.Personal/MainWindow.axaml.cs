using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace View.Personal
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var comboBox = this.FindControl<ComboBox>("ModelProviderComboBox");
            if (comboBox != null)
            {
                Console.WriteLine($"ModelProviderComboBox found with {comboBox.Items.Count} items");
                comboBox.SelectedIndex = 0; // Explicitly set to OpenAI
                UpdateSettingsVisibility("OpenAI");
            }
            else
            {
                Console.WriteLine("ModelProviderComboBox not found!");
            }
        }

        private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem selectedItem)
            {
                string? selectedContent = selectedItem.Content?.ToString();
                WorkspaceText.Text = selectedContent;

                DashboardPanel.IsVisible = selectedContent == "Dashboard";
                SettingsPanel.IsVisible = selectedContent == "Settings";
                WorkspaceText.IsVisible = !(DashboardPanel.IsVisible || SettingsPanel.IsVisible);
            }
        }

        private void ModelProvider_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedProvider = selectedItem.Content.ToString();
                UpdateSettingsVisibility(selectedProvider);
            }
        }

        private void UpdateSettingsVisibility(string selectedProvider)
        {
            if (OpenAISettings != null)
                OpenAISettings.IsVisible = selectedProvider == "OpenAI";
            if (AnthropicSettings != null)
                AnthropicSettings.IsVisible = selectedProvider == "Anthropic";
            if (ViewSettings != null)
                ViewSettings.IsVisible = selectedProvider == "View";
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var selectedProvider = (this.FindControl<ComboBox>("ModelProviderComboBox").SelectedItem as ComboBoxItem)?.Content.ToString();

            switch (selectedProvider)
            {
                case "OpenAI":
                    var openAIKey = this.FindControl<TextBox>("OpenAIKey").Text;
                    var embeddingModel = this.FindControl<TextBox>("OpenAIEmbeddingModel").Text;
                    var completionModel = this.FindControl<TextBox>("OpenAICompletionModel").Text;
                    Console.WriteLine($"Saving OpenAI: Key={openAIKey}, Embedding={embeddingModel}, Completion={completionModel}");
                    break;
                case "Anthropic":
                    var voyageKey = this.FindControl<TextBox>("VoyageAIKey").Text;
                    var voyageEmbeddingModel = this.FindControl<TextBox>("VoyageAIEmbeddingModel").Text;
                    var anthropicKey = this.FindControl<TextBox>("AnthropicKey").Text;
                    var anthropicModel = this.FindControl<TextBox>("AnthropicCompletionModel").Text;
                    Console.WriteLine($"Saving Anthropic: VoyageKey={voyageKey}, VoyageModel={voyageEmbeddingModel}, AnthropicKey={anthropicKey}, AnthropicModel={anthropicModel}");
                    break;
                case "View":
                    var embeddingsUrl = this.FindControl<TextBox>("EmbeddingsServerUrl").Text;
                    var embeddingsKey = this.FindControl<TextBox>("EmbeddingsApiKey").Text;
                    var generatorType = this.FindControl<TextBox>("EmbeddingsGeneratorType").Text;
                    var chatUrl = this.FindControl<TextBox>("ChatUrl").Text;
                    var chatKey = this.FindControl<TextBox>("ChatApiKey").Text;
                    var presetGuid = this.FindControl<TextBox>("PresetGuid").Text;
                    Console.WriteLine($"Saving View: EmbeddingsUrl={embeddingsUrl}, EmbeddingsKey={embeddingsKey}, Generator={generatorType}, ChatUrl={chatUrl}, ChatKey={chatKey}, PresetGuid={presetGuid}");
                    break;
            }
        }

        private void NavigateToSettings_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "Settings") is ListBoxItem settingsItem)
            {
                NavList.SelectedItem = settingsItem;
            }
        }

        private void NavigateToMyFiles_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "My Files") is ListBoxItem myFilesItem)
            {
                NavList.SelectedItem = myFilesItem;
            }
        }

        private void NavigateToChat_Click(object sender, RoutedEventArgs e)
        {
            if (NavList.Items.OfType<ListBoxItem>().FirstOrDefault(x => x.Content?.ToString() == "Chat") is ListBoxItem chatItem)
            {
                NavList.SelectedItem = chatItem;
            }
        }
    }
}