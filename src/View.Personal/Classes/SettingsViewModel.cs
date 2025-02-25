using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace View.Personal.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning restore CS8612 // Nullability of reference types in type doesn't match implicitly implemented member.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}