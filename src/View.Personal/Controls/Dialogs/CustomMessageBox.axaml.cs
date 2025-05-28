using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Material.Icons;
using Material.Icons.Avalonia;
using System.Threading.Tasks;
using View.Personal.Classes;
using View.Personal.Enums;

namespace View.Personal.Controls.Dialogs
{
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
                WindowState = WindowState.Normal,
            };

            var tcs = new TaskCompletionSource<ButtonResult>();
            window.DataContext = tcs;

            window.Closed += (_, _) =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.SetResult(ButtonResult.Cancel);
            };

            window.Show();
            return tcs.Task;
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
                //MaxWidth = 520
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
                HorizontalAlignment = HorizontalAlignment.Center
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
                        if (window.DataContext is TaskCompletionSource<ButtonResult> tcs && !tcs.Task.IsCompleted)
                        {
                            tcs.SetResult(result);
                        }
                        window.Close();
                    }
                };

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