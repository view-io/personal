namespace View.Personal.Helpers
{
    using Avalonia.Controls;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using View.Personal.Classes;
    using View.Personal.Controls.Dialogs;
    using View.Personal.Enums;

    /// <summary>
    /// Helper class for creating styled message boxes that match the application's UI.
    /// </summary>
    public static class CustomMessageBoxHelper
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Shows a styled standard message box asynchronously.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="text">The message text.</param>
        /// <param name="buttons">The type of buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <param name="textLines">Optional list of text lines to display in the message box. Each string will be displayed on a separate line.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task<ButtonResult> ShowMessageBoxAsync(
            string title,
            string text,
            MessageBoxButtons buttons = MessageBoxButtons.Ok,
            MessageBoxIcon icon = MessageBoxIcon.None,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner,
            List<string> textLines = null!)
        {
            // If textLines is provided, use it to create the message text
            if (textLines != null && textLines.Count > 0)
            {
                text = string.Join("\n", textLines);
            }

            var parameters = new CustomMessageBoxParams
            {
                Title = title,
                Message = text,
                Icon = icon,
                WindowStartupLocation = windowStartupLocation,
                Buttons = GetButtonDefinitions(buttons),
                TextLines = textLines ?? new List<string>()
            };

            return CustomMessageBox.ShowAsync(parameters);
        }

        /// <summary>
        /// Shows a confirmation message box with Yes/No buttons.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="text">The message text.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <param name="textLines">Optional list of text lines to display in the message box. Each string will be displayed on a separate line.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task<ButtonResult> ShowConfirmationAsync(
            string title,
            string text,
            MessageBoxIcon icon = MessageBoxIcon.Question,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner,
            List<string> textLines = null!)
        {
            if (textLines != null && textLines.Count > 0)
            {
                text += "\n" + string.Join("\n", textLines);
            }

            var parameters = new CustomMessageBoxParams
            {
                Title = title,
                Message = text,
                Icon = icon,
                WindowStartupLocation = windowStartupLocation,
                Buttons = GetButtonDefinitions(MessageBoxButtons.YesNo),
                TextLines = textLines ?? new List<string>()
            };

            return CustomMessageBox.ShowAsync(parameters);
        }

        /// <summary>
        /// Shows an error message box.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="text">The error message.</param>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task<ButtonResult> ShowErrorAsync(
            string title,
            string text,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            return ShowMessageBoxAsync(title, text, MessageBoxButtons.Ok, MessageBoxIcon.Error, windowStartupLocation);
        }

        /// <summary>
        /// Shows a message box with a clickable link to download or install a service.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="message">The message text.</param>
        /// <param name="linkText">The text for the clickable link.</param>
        /// <param name="linkUrl">The URL to open when the link is clicked.</param>
        /// <param name="buttons">The type of buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <param name="textLines">Optional list of text lines to display in the message box. Each string will be displayed on a separate line.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task<ButtonResult> ShowServiceNotInstalledAsync(
            string title,
            string message,
            string linkText,
            string linkUrl,
            MessageBoxButtons buttons = MessageBoxButtons.Ok,
            MessageBoxIcon icon = MessageBoxIcon.Warning,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner,
            List<string> textLines = null!)
        {
            if (textLines != null && textLines.Count > 0)
            {
                message += "\n" + string.Join("\n", textLines);
            }
            var parameters = new CustomMessageBoxParams
            {
                Title = title,
                Message = message,
                Icon = icon,
                LinkText = linkText,
                LinkUrl = linkUrl,
                WindowStartupLocation = windowStartupLocation,
                TextLines = textLines ?? new List<string>(),
                Buttons = GetButtonDefinitions(buttons),
            };

            return CustomMessageBox.ShowServiceNotInstalledAsync(parameters);
        }

        /// <summary>
        /// Shows a warning message box.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="text">The warning message.</param>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task<ButtonResult> ShowWarningAsync(
            string title,
            string text,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            return ShowMessageBoxAsync(title, text, MessageBoxButtons.Ok, MessageBoxIcon.Warning, windowStartupLocation);
        }

        /// <summary>
        /// Shows a dialog with an input field for user text entry.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="prompt">The prompt text for the input field.</param>
        /// <param name="defaultValue">The default value for the input field.</param>
        /// <param name="enableValidation">Whether to enable input validation to prevent empty or whitespace-only input.</param>
        /// <param name="validationErrorMessage">The error message to display when validation fails.</param>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the input text and button result.</returns>
        public static Task<(string Text, ButtonResult Result)> ShowInputDialogAsync(
            string title = "Input Dialog",
            string prompt = "Enter value:",
            string defaultValue = "",
            bool enableValidation = false,
            string validationErrorMessage = "Please enter a value",
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            var parameters = new CustomMessageBoxParams
            {
                Title = title,
                InputPrompt = prompt,
                InputDefaultValue = defaultValue,
                WindowStartupLocation = windowStartupLocation,
                EnableInputValidation = enableValidation,
                ValidationErrorMessage = validationErrorMessage,
                Buttons = new List<ButtonDefinition>
                {
                    new ButtonDefinition(Services.ResourceManagerService.GetString("OK"), ButtonResult.Ok),
                    new ButtonDefinition(Services.ResourceManagerService.GetString("Cancel"), ButtonResult.Cancel)
                }
            };

            return CustomMessageBox.ShowWithInputAsync(parameters);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Gets the button definitions based on the button type.
        /// </summary>
        /// <param name="buttons">The button type.</param>
        /// <returns>A list of button definitions.</returns>
        private static List<ButtonDefinition> GetButtonDefinitions(MessageBoxButtons buttons)
        {
            var buttonDefinitions = new List<ButtonDefinition>();

            switch (buttons)
            {
                case MessageBoxButtons.Ok:
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("OK"), ButtonResult.Ok));
                    break;
                case MessageBoxButtons.OkCancel:
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("OK"), ButtonResult.Ok));
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Cancel"), ButtonResult.Cancel));
                    break;
                case MessageBoxButtons.YesNo:
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Yes"), ButtonResult.Yes));
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("No"), ButtonResult.No));
                    break;
                case MessageBoxButtons.YesNoCancel:
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Yes"), ButtonResult.Yes));
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("No"), ButtonResult.No));
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Cancel"), ButtonResult.Cancel));
                    break;
                case MessageBoxButtons.AbortRetryIgnore:
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Abort"), ButtonResult.Abort));
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Retry"), ButtonResult.Retry));
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Ignore"), ButtonResult.Ignore));
                    break;
                case MessageBoxButtons.RetryCancel:
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Retry"), ButtonResult.Retry));
                    buttonDefinitions.Add(new ButtonDefinition(Services.ResourceManagerService.GetString("Cancel"), ButtonResult.Cancel));
                    break;
            }

            return buttonDefinitions;
        }

        #endregion
    }
}