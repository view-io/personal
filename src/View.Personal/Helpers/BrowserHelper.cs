namespace View.Personal.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Helper class for opening URLs in the default browser.
    /// </summary>
    public static class BrowserHelper
    {
        /// <summary>
        /// Opens the specified URL in the default browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public static bool OpenUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return false;

                // Use the appropriate method based on the operating system
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS
                    Process.Start("open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Linux
                    Process.Start("xdg-open", url);
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                App? app = App.Current as App;
                app?.Log(Enums.SeverityEnum.Error, $"Failed to open URL: {ex.Message}");
                app?.LogExceptionToFile(ex,$"Failed to open URL");
                return false;
            }
        }
    }
}