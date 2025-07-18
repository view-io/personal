namespace ViewPersonal.Updater
{
    using System;
    using System.IO;

    internal static class Constants
    {
        internal static string VersionCheckApiUrl = "http://desktop-personal-versions.view.io:5000/api/Versions/latest";

        internal static string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ViewPersonal",
            "logs");

        internal static int VersionCheckDelayMilliseconds = 30000;
    }
}