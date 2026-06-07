using System;
using System.IO;
using UnityEngine;

namespace ModLoader
{
    /// <summary>
    /// Logger for mods that logs to both Unity console and file
    /// </summary>
    public class ModLogger
    {
        private readonly string modName;
        private readonly string logFilePath;
        private readonly object lockObject = new object();

        public ModLogger(string modName)
        {
            this.modName = modName;

            // Create Mods/Logs directory if it doesn't exist
            string logsDirectory = Path.Combine(Application.dataPath, "..", "Mods", "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            // Set log file path
            logFilePath = Path.Combine(logsDirectory, $"{SanitizeFileName(modName)}.log");

            // Write header to log file
            WriteToFile($"=== {modName} Log Started at {DateTime.Now} ===");
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public void Info(string message)
        {
            Log("INFO", message, LogType.Log);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void Warning(string message)
        {
            Log("WARNING", message, LogType.Warning);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void Error(string message)
        {
            Log("ERROR", message, LogType.Error);
        }

        /// <summary>
        /// Log an exception
        /// </summary>
        public void Error(Exception exception)
        {
            Error($"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
        }

        private void Log(string level, string message, LogType logType)
        {
            string formattedMessage = $"[{modName}] [{level}] {message}";
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {formattedMessage}";

            // Log to Unity console
            switch (logType)
            {
                case LogType.Error:
                    ConsoleBase.WriteError(formattedMessage);
                    break;
                case LogType.Warning:
                    ConsoleBase.WriteWarn(formattedMessage);
                    break;
                default:
                    ConsoleBase.WriteLine(formattedMessage);
                    break;
            }

            // Log to file
            WriteToFile(timestampedMessage);
        }

        private void WriteToFile(string message)
        {
            lock (lockObject)
            {
                try
                {
                    File.AppendAllText(logFilePath, message + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    ConsoleBase.WriteLine($"[ModLoader] Failed to write to log file for {modName}: {ex.Message}");
                }
            }
        }

        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }
}

