using System;
using System.IO;

namespace ViewPersonal.Updater
{
    internal static class Constants
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";
        
        internal static string VersionCheckApiUrl = "http://192.168.101.190:8055/api/Versions/latest";
        
        internal static string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "ViewPersonal", 
            "logs");
            
        internal static string LogFilePath = Path.Combine(LogDirectory, "updater.log");
        
        internal static int VersionCheckDelayMilliseconds = 30000;
    }
}