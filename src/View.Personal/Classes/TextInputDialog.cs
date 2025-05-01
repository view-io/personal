using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using System.Threading.Tasks;

public class TextInputDialog : Window
{
    private readonly TextBox _textBox;
    private string _result;

    public TextInputDialog(string title, string prompt)
    {
        Title = title;
        Width = 300;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var stackPanel = new StackPanel { Spacing = 10, Margin = new Thickness(20) };
        stackPanel.Children.Add(new TextBlock { Text = prompt });

        _textBox = new TextBox { Text = "" };
        stackPanel.Children.Add(_textBox);

        var buttonPanel = new StackPanel
            { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        var okButton = new Button { Content = "OK" };
        var cancelButton = new Button { Content = "Cancel" };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(buttonPanel);
        Content = stackPanel;

        okButton.Click += (_, __) =>
        {
            _result = _textBox.Text;
            Close();
        };
        cancelButton.Click += (_, __) =>
        {
            _result = null;
            Close();
        };
    }

    public async Task<string> ShowDialogAsync(Window owner)
    {
        await ShowDialog(owner);
        return _result;
    }
}