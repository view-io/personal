using System;
using System.IO;

namespace View.Personal
{
    internal static class Constants
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string LiteGraphDatabaseFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data", "view-personal.db");

    }
}