using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace View.Personal.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _selectedProvider = "OpenAI";
        private bool _isOpenAIVisible = true;
        private bool _isAnthropicVisible = false;
        private bool _isViewVisible = false;

        public string SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                _selectedProvider = value;
                UpdateVisibility();
                OnPropertyChanged();
            }
        }

        public bool IsOpenAIVisible
        {
            get => _isOpenAIVisible;
            set { _isOpenAIVisible = value; OnPropertyChanged(); }
        }

        public bool IsAnthropicVisible
        {
            get => _isAnthropicVisible;
            set { _isAnthropicVisible = value; OnPropertyChanged(); }
        }

        public bool IsViewVisible
        {
            get => _isViewVisible;
            set { _isViewVisible = value; OnPropertyChanged(); }
        }

        private void UpdateVisibility()
        {
            IsOpenAIVisible = _selectedProvider == "OpenAI";
            IsAnthropicVisible = _selectedProvider == "Anthropic";
            IsViewVisible = _selectedProvider == "View";
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}