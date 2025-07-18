namespace View.Personal
{
    using System;
    using System.IO;
    internal static class Constants
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string LiteGraphDatabaseFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data", "view-personal.db");

    }
}