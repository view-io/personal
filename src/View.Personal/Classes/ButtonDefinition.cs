namespace View.Personal.Classes
{
    using View.Personal.Enums;

    /// <summary>
    /// Represents a button definition for a custom message box.
    /// </summary>
    public class ButtonDefinition
    {
        /// <summary>
        /// Gets or sets the text of the button.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the result of the button.
        /// </summary>
        public ButtonResult Result { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonDefinition"/> class.
        /// </summary>
        /// <param name="text">The text of the button.</param>
        /// <param name="result">The result of the button.</param>
        public ButtonDefinition(string text, ButtonResult result)
        {
            Text = text;
            Result = result;
        }
    }
}
