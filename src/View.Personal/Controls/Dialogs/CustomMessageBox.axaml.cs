namespace View.Personal.Controls.Dialogs
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Layout;
    using Avalonia.Media;
    using Avalonia.Platform;
    using Avalonia.VisualTree;
    using Material.Icons;
    using Material.Icons.Avalonia;
    using System.Linq;
    using System.Threading.Tasks;
    using View.Personal.Classes;
    using View.Personal.Enums;

    /// <summary>
    /// A customizable modal message box for displaying alerts, confirmations, and prompts in the View.Personal application.
    /// </summary>
    public partial class CustomMessageBox : UserControl
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMessageBox"/> class.
        /// </summary>
        public CustomMessageBox()
        {
            InitializeComponent();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Shows a message box with the specified parameters.
        /// </summary>
        /// <param name="params">The parameters for the message box.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the button result.</returns>
        public static Task<ButtonResult> ShowAsync(CustomMessageBoxParams @params)
        {
            return ShowAsyncInternal(@params).ContinueWith(t => t.Result.Result);
        }

        /// <summary>
        /// Shows a message box with the specified parameters and an input field.
        /// </summary>
        /// <param name="params">The parameters for the message box.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the input text and button result.</returns>
        public static Task<(string Text, ButtonResult Result)> ShowWithInputAsync(CustomMessageBoxParams @params)
        {
            @params.HasInputField = true;
            return ShowAsyncInternal(@params);
        }

        /// <summary>
        /// Internal method that shows a message box with the specified parameters.
        /// </summary>
        /// <param name="params">The parameters for the message box.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the input text and button result.</returns>
        private static async Task<(string Text, ButtonResult Result)> ShowAsyncInternal(CustomMessageBoxParams @params)
        {
            var window = new Window
            {
                Title = @params.Title,
                Content = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    ClipToBounds = true,
                    Child = CreateMessageBoxContent(@params)
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                WindowStartupLocation = @params.WindowStartupLocation,
                MinWidth = 380,
                Classes = { "messageBox" },
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                TransparencyLevelHint = new[]
                {
                      WindowTransparencyLevel.AcrylicBlur,
                      WindowTransparencyLevel.Transparent,
                      WindowTransparencyLevel.None
                },
                ExtendClientAreaToDecorationsHint = true,
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
                ShowInTaskbar = false,
                Topmost = false,
                WindowState = WindowState.Normal,
            };

            var tcs = new TaskCompletionSource<(string Text, ButtonResult Result)>();
            window.DataContext = tcs;

            window.Closed += (_, _) =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.SetResult((string.Empty, ButtonResult.Cancel));
            };

            var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow != null)
            {
                await window.ShowDialog(desktop.MainWindow);
            }
            else
            {
                window.Show();
            }
            return await tcs.Task;
        }

        /// <summary>
        /// Shows a message box with a clickable link to download or install a service.
        /// </summary>
        /// <param name="params">The parameters for the message box.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task<ButtonResult> ShowServiceNotInstalledAsync(CustomMessageBoxParams @params)
        {
            var window = new Window
            {
                Title = @params.Title,
                Content = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    ClipToBounds = true,
                    Child = CreateMessageBoxContent(@params)
                },
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                WindowStartupLocation = @params.WindowStartupLocation,
                MinWidth = 380,
                Classes = { "messageBox" },
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                TransparencyLevelHint = new[]
                {
                      WindowTransparencyLevel.AcrylicBlur,
                      WindowTransparencyLevel.Transparent,
                      WindowTransparencyLevel.None
                },
                ExtendClientAreaToDecorationsHint = true,
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
                ShowInTaskbar = true,
                Icon = null,
                Topmost = false,
                WindowState = WindowState.Normal,
            };

            var tcs = new TaskCompletionSource<ButtonResult>();
            window.DataContext = tcs;

            window.Closed += (_, _) =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.SetResult(ButtonResult.Cancel);
            };

            var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow != null)
            {
                await window.ShowDialog(desktop.MainWindow);
            }
            else
            {
                window.Show();
            }
            return await tcs.Task;
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Creates the message box content layout.
        /// </summary>
        /// <param name="params">The parameters for configuring the message box.</param>
        /// <returns>A <see cref="Border"/> that wraps the message box content.</returns>
        private static Border CreateMessageBoxContent(CustomMessageBoxParams @params)
        {
            var contentBorder = new Border
            {
                MinWidth = 360,
            };

            var mainPanel = new StackPanel
            {
                Spacing = 0,
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var titlePanel = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#F8F8F8")),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(0, 16, 0, 16)
            };

            var titleBlock = new TextBlock
            {
                Text = @params.Title,
                Classes = { "title" },
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(16, 0, 16, 0),
                FontWeight = FontWeight.SemiBold,
                FontSize = 16
            };

            var titleStackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            titleStackPanel.Children.Add(titleBlock);
            titlePanel.Child = titleStackPanel;
            mainPanel.Children.Add(titlePanel);

            var contentPanel = new StackPanel
            {
                Spacing = 16,
                Margin = new Thickness(24, 32, 24, 24),
                HorizontalAlignment = @params.HasInputField ? HorizontalAlignment.Stretch : HorizontalAlignment.Center
            };

            if (@params.Icon != MessageBoxIcon.None)
            {
                var materialIcon = new MaterialIcon
                {
                    Width = 64,
                    Height = 64,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                materialIcon.Kind = @params.Icon switch
                {
                    MessageBoxIcon.Info => MaterialIconKind.InformationOutline,
                    MessageBoxIcon.Warning => MaterialIconKind.AlertOutline,
                    MessageBoxIcon.Error => MaterialIconKind.CloseCircleOutline,
                    MessageBoxIcon.Question => MaterialIconKind.HelpCircleOutline,
                    _ => MaterialIconKind.InformationOutline
                };

                materialIcon.Foreground = @params.Icon switch
                {
                    MessageBoxIcon.Info => new SolidColorBrush(Color.Parse("#0472EF")),
                    MessageBoxIcon.Warning => new SolidColorBrush(Color.Parse("#FF9800")),
                    MessageBoxIcon.Error => new SolidColorBrush(Color.Parse("#D94242")),
                    MessageBoxIcon.Question => new SolidColorBrush(Color.Parse("#0472EF")),
                    _ => new SolidColorBrush(Color.Parse("#0472EF"))
                };

                var iconContainer = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 16)
                };
                iconContainer.Children.Add(materialIcon);
                contentPanel.Children.Add(iconContainer);
            }

            var messageBlock = new TextBlock
            {
                Text = @params.Message,
                Classes = { "messageBoxText" },
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 14,
                LineHeight = 20,
                Foreground = new SolidColorBrush(Color.Parse("#505050"))
            };

            var messageContainer = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            messageContainer.Children.Add(messageBlock);
            contentPanel.Children.Add(messageContainer);

            // Add text input field if requested
            TextBox inputTextBox = null!;
            if (@params.HasInputField)
            {
                if (!string.IsNullOrEmpty(@params.InputPrompt))
                {
                    var promptBlock = new TextBlock
                    {
                        Text = @params.InputPrompt,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0)
                    };
                    contentPanel.Children.Add(promptBlock);
                }

                var inputContainer = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Spacing = 0
                };

                inputTextBox = new TextBox
                {
                    Text = @params.InputDefaultValue,
                    Classes = { "inputField" },
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 300,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    BorderBrush = new SolidColorBrush(Color.Parse("#0078D7"))
                };

                inputContainer.Children.Add(inputTextBox);

                // Create validation error message text block (initially hidden)
                var validationErrorBlock = new TextBlock
                {
                    Text = @params.ValidationErrorMessage,
                    Foreground = new SolidColorBrush(Color.Parse("#D94242")),
                    FontSize = 12,
                    IsVisible = false,
                    Margin = new Thickness(0, 4, 0, 0)
                };

                inputContainer.Children.Add(validationErrorBlock);
                contentPanel.Children.Add(inputContainer);
            }


            // Add clickable link if LinkText and LinkUrl are set
            if (!string.IsNullOrWhiteSpace(@params.LinkText) && !string.IsNullOrWhiteSpace(@params.LinkUrl))
            {
                var linkTextBlock = new TextBlock
                {
                    Text = @params.LinkText,
                    TextDecorations = TextDecorations.Underline,
                    Foreground = new SolidColorBrush(Color.Parse("#0472EF")),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 8)
                };

                linkTextBlock.PointerPressed += (_, _) =>
                {
                    Helpers.BrowserHelper.OpenUrl(@params.LinkUrl);
                };

                contentPanel.Children.Add(linkTextBlock);
            }


            mainPanel.Children.Add(contentPanel);

            var buttonPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(24, 8, 24, 24),
                Orientation = Orientation.Horizontal
            };

            foreach (var buttonDef in @params.Buttons)
            {
                var button = new Button
                {
                    Content = buttonDef.Text,
                    Classes = { "messageBoxButton" },
                    Tag = buttonDef.Result,
                    Height = 40,
                    MinWidth = 100,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 0, 16, 0),
                    Margin = new Thickness(8, 4, 8, 4)

                };

                button.Click += (sender, _) =>
                {
                    if (sender is Button clickedButton &&
                        clickedButton.Tag is ButtonResult result &&
                        clickedButton.FindAncestorOfType<Window>() is Window window)
                    {
                        if (window.DataContext is TaskCompletionSource<(string Text, ButtonResult Result)> tcs && !tcs.Task.IsCompleted)
                        {
                            string inputText = inputTextBox?.Text ?? string.Empty;

                            // Check if validation is enabled and this is the OK button
                            if (@params.EnableInputValidation &&
                                result == ButtonResult.Ok &&
                                string.IsNullOrWhiteSpace(inputText))
                            {
                                // Show validation error
                                if (inputTextBox != null && inputTextBox.Parent is StackPanel inputContainer)
                                {
                                    // Find the validation error text block
                                    var validationErrorBlock = inputContainer.Children
                                        .OfType<TextBlock>()
                                        .FirstOrDefault();

                                    if (validationErrorBlock != null)
                                    {
                                        validationErrorBlock.IsVisible = true;
                                    }

                                    // Change input border to red
                                    inputTextBox.BorderBrush = new SolidColorBrush(Color.Parse("#D94242"));
                                }

                                // Don't close the dialog
                                return;
                            }

                            tcs.SetResult((result == ButtonResult.Cancel ? string.Empty : inputText, result));
                        }
                        window.Close();
                    }
                };

                // If this is an input dialog with validation, add text changed handler to reset validation state
                if (@params.EnableInputValidation && inputTextBox != null && buttonDef.Result == ButtonResult.Ok)
                {
                    inputTextBox.TextChanged += (sender, _) =>
                    {
                        if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text) &&
                            textBox.Parent is StackPanel inputContainer)
                        {
                            // Reset border color to blue
                            textBox.BorderBrush = new SolidColorBrush(Color.Parse("#0078D7"));

                            // Hide validation error message
                            var validationErrorBlock = inputContainer.Children
                                .OfType<TextBlock>()
                                .FirstOrDefault();

                            if (validationErrorBlock != null)
                            {
                                validationErrorBlock.IsVisible = false;
                            }
                        }
                    };
                }

                button.Background = Brushes.White;
                button.BorderBrush = new SolidColorBrush(Color.Parse("#EEEEEE"));
                button.Foreground = new SolidColorBrush(Color.Parse("#505050"));

                buttonPanel.Children.Add(button);
            }

            mainPanel.Children.Add(buttonPanel);

            contentBorder.Child = mainPanel;
            return contentBorder;
        }

        #endregion
    }
}