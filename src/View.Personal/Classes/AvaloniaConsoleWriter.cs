// ReSharper disable CheckNamespace

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
namespace View.Personal.Classes
{
    using Avalonia.Controls;
    using Avalonia.Threading;
    using System;
    using System.IO;
    using System.Text;

    public class AvaloniaConsoleWriter : TextWriter
    {
        #region Public-Members

        public override Encoding Encoding => Encoding.UTF8;

        #endregion

        #region Private-Members

        private readonly TextBox _TextBox;

        #endregion

        #region Constructors-and-Factories

        public AvaloniaConsoleWriter(TextBox textBox)
        {
            _TextBox = textBox;
        }

        #endregion

        #region Public-Methods

        public override void WriteLine(string value)
        {
            // Console.WriteLine on background threads.
            Dispatcher.UIThread.Post(() =>
            {
                _TextBox.Text += value + Environment.NewLine;
                _TextBox.CaretIndex = _TextBox.Text.Length;
            });
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}