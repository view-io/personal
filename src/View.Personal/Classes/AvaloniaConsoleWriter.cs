namespace View.Personal.Classes
{
    using Avalonia.Controls;
    using Avalonia.Threading;
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A custom TextWriter implementation that redirects console output to an Avalonia TextBox control.
    /// This allows console output to be displayed within the UI of an Avalonia application.
    /// </summary>
    public class AvaloniaConsoleWriter : TextWriter
    {
        // ReSharper disable CheckNamespace
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

        #region Public-Members

        /// <summary>
        /// Gets the character encoding in which the output is written.
        /// This implementation returns UTF-8 encoding.
        /// </summary>
        public override Encoding Encoding => Encoding.UTF8;

        #endregion

        #region Private-Members

        private readonly TextBox _TextBox;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the AvaloniaConsoleWriter class with the specified TextBox.
        /// </summary>
        /// <param name="textBox">The TextBox control where console output will be displayed.</param>
        /// <exception cref="ArgumentNullException">Thrown when textBox is null.</exception>
        public AvaloniaConsoleWriter(TextBox textBox)
        {
            _TextBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Writes a string followed by a line terminator to the text box on the UI thread.
        /// This method safely updates the UI by dispatching the update operation to the UI thread.
        /// </summary>
        /// <param name="value">The string to write to the text box.</param>
        public override void WriteLine(string? value)
        {
            if (value == null) return;
            Dispatcher.UIThread.Post(() =>
            {
                _TextBox.Text += value + Environment.NewLine;
                _TextBox.CaretIndex = _TextBox.Text.Length;
            });
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    }
}