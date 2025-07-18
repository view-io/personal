namespace View.Personal.Classes
{
    using Avalonia.Controls;
    using System.Collections.Generic;
    using View.Personal.Enums;

    /// <summary>
    /// Represents the parameters for a custom message box.
    /// </summary>
    public class CustomMessageBoxParams
    {
        /// <summary>
        /// Gets or sets the title of the message box.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon to display.
        /// </summary>
        public MessageBoxIcon Icon { get; set; } = MessageBoxIcon.None;

        /// <summary>
        /// Gets or sets the window startup location.
        /// </summary>
        public WindowStartupLocation WindowStartupLocation { get; set; } = WindowStartupLocation.CenterOwner;

        /// <summary>
        /// Gets or sets the buttons to display.
        /// </summary>
        public List<ButtonDefinition> Buttons { get; set; } = new List<ButtonDefinition>();

        /// <summary>
        /// Gets or sets a value indicating whether the message box should include a text input field.
        /// </summary>
        public bool HasInputField { get; set; } = false;

        /// <summary>
        /// Gets or sets the prompt text for the input field.
        /// </summary>
        public string InputPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default value for the input field.
        /// </summary>
        public string InputDefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether input validation is enabled.
        /// </summary>
        public bool EnableInputValidation { get; set; } = false;

        /// <summary>
        /// Gets or sets the validation error message to display when input is invalid.
        /// </summary>
        public string ValidationErrorMessage { get; set; } = "Please enter a value";

        /// <summary>
        /// Gets or sets the display text for the optional hyperlink in the message box.
        /// When set, this text will be shown as a clickable link.
        /// </summary>
        public string LinkText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL to navigate to when the hyperlink is clicked.
        /// This should be a valid web address (e.g., https://example.com).
        /// </summary>
        public string LinkUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the input field is currently in an error state.
        /// </summary>
        public bool IsInputInvalid { get; set; } = false;

        /// <summary>
        /// Gets or sets a list of text lines to display in the message box.
        /// If provided, each string in the list will be displayed on a separate line.
        /// </summary>
        public List<string> TextLines { get; set; } = new List<string>();
    }
}
