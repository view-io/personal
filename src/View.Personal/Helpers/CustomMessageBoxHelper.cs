using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using View.Personal.Classes;
using View.Personal.Controls;
using View.Personal.Controls.Dialogs;
using View.Personal.Enums;

namespace View.Personal.Helpers
{
    /// <summary>
    /// Helper class for creating styled message boxes that match the application's UI.
    /// </summary>
    public static class CustomMessageBoxHelper
    {
        /// <summary>
        /// Shows a styled standard message box asynchronously.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="text">The message text.</param>
        /// <param name="buttons">The type of buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="windowStartupLocation">The startup location of the window.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task<ButtonResult> ShowMessageBoxAsync(
            string title,
            string text,
            MessageBoxButtons buttons = MessageBoxButtons.Ok,
            MessageBoxIcon icon = MessageBoxIcon.None,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            var parameters = new CustomMessageBoxParams
            {
                Title = title,
                Message = text,
                Icon = icon,
                WindowStartupLocation = windowStartupLocation,
                Buttons = GetButtonDefinitions(buttons)
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
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static Task<ButtonResult> ShowConfirmationAsync(
            string title,
            string text,
            MessageBoxIcon icon = MessageBoxIcon.Question,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            return ShowMessageBoxAsync(title, text, MessageBoxButtons.YesNo, icon, windowStartupLocation);
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
                    buttonDefinitions.Add(new ButtonDefinition("OK", ButtonResult.Ok));
                    break;
                case MessageBoxButtons.OkCancel:
                    buttonDefinitions.Add(new ButtonDefinition("OK", ButtonResult.Ok));
                    buttonDefinitions.Add(new ButtonDefinition("Cancel", ButtonResult.Cancel));
                    break;
                case MessageBoxButtons.YesNo:
                    buttonDefinitions.Add(new ButtonDefinition("Yes", ButtonResult.Yes));
                    buttonDefinitions.Add(new ButtonDefinition("No", ButtonResult.No));
                    break;
                case MessageBoxButtons.YesNoCancel:
                    buttonDefinitions.Add(new ButtonDefinition("Yes", ButtonResult.Yes));
                    buttonDefinitions.Add(new ButtonDefinition("No", ButtonResult.No));
                    buttonDefinitions.Add(new ButtonDefinition("Cancel", ButtonResult.Cancel));
                    break;
                case MessageBoxButtons.AbortRetryIgnore:
                    buttonDefinitions.Add(new ButtonDefinition("Abort", ButtonResult.Abort));
                    buttonDefinitions.Add(new ButtonDefinition("Retry", ButtonResult.Retry));
                    buttonDefinitions.Add(new ButtonDefinition("Ignore", ButtonResult.Ignore));
                    break;
                case MessageBoxButtons.RetryCancel:
                    buttonDefinitions.Add(new ButtonDefinition("Retry", ButtonResult.Retry));
                    buttonDefinitions.Add(new ButtonDefinition("Cancel", ButtonResult.Cancel));
                    break;
            }

            return buttonDefinitions;
        }
    }

    /// <summary>
    /// Represents the type of buttons to display in a message box.
    /// </summary>
    public enum MessageBoxButtons
    {
        /// <summary>
        /// The message box contains an OK button.
        /// </summary>
        Ok,

        /// <summary>
        /// The message box contains OK and Cancel buttons.
        /// </summary>
        OkCancel,

        /// <summary>
        /// The message box contains Yes and No buttons.
        /// </summary>
        YesNo,

        /// <summary>
        /// The message box contains Yes, No, and Cancel buttons.
        /// </summary>
        YesNoCancel,

        /// <summary>
        /// The message box contains Abort, Retry, and Ignore buttons.
        /// </summary>
        AbortRetryIgnore,

        /// <summary>
        /// The message box contains Retry and Cancel buttons.
        /// </summary>
        RetryCancel
    }
}