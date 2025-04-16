using Avalonia.Controls;
using Avalonia.Threading;
using System;

public class LoggingService
{
    private readonly TextBox _ConsoleOutput;
    private readonly Window _Window;

    public LoggingService(Window window, TextBox consoleOutput)
    {
        _Window = window;
        _ConsoleOutput = consoleOutput;
    }

    public void Log(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _ConsoleOutput.Text += message + "\n";
            if (_ConsoleOutput.Parent is ScrollViewer scrollViewer) scrollViewer.ScrollToEnd();
        });
        Console.WriteLine(message);
    }
}