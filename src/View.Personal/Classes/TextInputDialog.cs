namespace View.Personal.Classes
{
    using Avalonia.Controls;
    using Avalonia.Layout;
    using Avalonia;
    using System.Threading.Tasks;

    /// <summary>
    /// A dialog window for collecting text input from the user with OK and Cancel options.
    /// </summary>
    /// <remarks>
    /// This dialog displays a prompt, a text input field, and two buttons (OK and Cancel). It is designed to be shown modally,
    /// allowing the user to enter text and confirm or cancel the input. The result is returned asynchronously when the dialog is closed.
    /// </remarks>
    public class TextInputDialog : Window
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        private readonly TextBox _TextBox;
        private string _Result;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextInputDialog"/> class with a specified title and prompt.
        /// </summary>
        /// <param name="title">The title of the dialog window.</param>
        /// <param name="prompt">The prompt text displayed above the input field.</param>
        public TextInputDialog(string title, string prompt)
        {
            Title = title;
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel { Spacing = 10, Margin = new Thickness(20) };
            stackPanel.Children.Add(new TextBlock { Text = prompt });

            _TextBox = new TextBox { Text = "" };
            stackPanel.Children.Add(_TextBox);

            var buttonPanel = new StackPanel
                { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = Services.ResourceManagerService.GetString("OK") };
            var cancelButton = new Button { Content = Services.ResourceManagerService.GetString("Cancel") };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(buttonPanel);
            Content = stackPanel;

            okButton.Click += (_, __) =>
            {
                _Result = _TextBox.Text;
                Close();
            };
            cancelButton.Click += (_, __) =>
            {
                _Result = null;
                Close();
            };
        }

        /// <summary>
        /// Displays the dialog modally and returns the user's input text asynchronously.
        /// </summary>
        /// <param name="owner">The parent window that owns this dialog.</param>
        /// <returns>A <see cref="Task{String}"/> representing the asynchronous operation, returning the entered text or null if canceled.</returns>
        public async Task<string> ShowDialogAsync(Window owner)
        {
            await ShowDialog(owner);
            return _Result;
        }

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
}