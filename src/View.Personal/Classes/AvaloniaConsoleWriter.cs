using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.IO;
using System.Text;

// ReSharper disable CheckNamespace
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

public class AvaloniaConsoleWriter : TextWriter
{
    private readonly TextBox _TextBox;

    public AvaloniaConsoleWriter(TextBox textBox)
    {
        _TextBox = textBox;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string value)
    {
        // Console.WriteLine on background threads.
        Dispatcher.UIThread.Post(() =>
        {
            _TextBox.Text += value + Environment.NewLine;
            _TextBox.CaretIndex = _TextBox.Text.Length;
        });
    }
}