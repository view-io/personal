using System;
using System.IO;

namespace View.Personal
{
    internal static class Constants
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string LiteGraphDatabaseFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "data", "view-personal.db");

        internal static string VoskModelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "VoskModels", "vosk-model-en-us-0.22");
        
        internal static string VoskModelZipPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViewPersonal", "VoskModels", "vosk-model-en-us-0.22.zip");
        
        internal static string VoskModelUrl = "https://alphacephei.com/vosk/models/vosk-model-en-us-0.22.zip";
    }
}