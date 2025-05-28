using Avalonia.Controls;
using System.Collections.Generic;
using View.Personal.Controls;
using View.Personal.Enums;

namespace View.Personal.Classes
{
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
    }
}
