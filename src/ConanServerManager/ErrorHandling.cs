using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using ServerManagerTool.Common.Utils;

namespace ServerManagerTool
{
    public static class ErrorHandling
    {
        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Oops!  Bad news everyone - the app is going down!
            // Write out a log file with all the details so users can send us the info...
            
            var file = Path.GetTempFileName();

            try
            {
                var details = new StringBuilder();
                details.AppendLine("Server Manager Crash Report");
                details.AppendLine($"Please report this crash to the Server Manager discord - {Config.Default.DiscordUrl}");
                details.AppendLine();

                details.AppendLine($"Assembly: {Assembly.GetEntryAssembly()}");
                details.AppendLine($"Version: {App.Instance.Version}");
                details.AppendLine($"Code: {Config.Default.ServerManagerCode}");
                details.AppendLine($"IsAdministrator: {SecurityUtils.IsAdministrator()}");
                details.AppendLine();

                details.AppendLine($"Windows Platform: {Environment.OSVersion.Platform}");
                details.AppendLine($"Windows Version: {Environment.OSVersion.VersionString}");
                details.AppendLine();

                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    details.AppendLine("Exception Message:");
                    details.AppendLine(exception.Message);
                    details.AppendLine();

                    details.AppendLine("Stack Trace:");
                    details.AppendLine(exception.StackTrace);
                }

                File.WriteAllText(file, details.ToString());

                var message = new StringBuilder();
                message.AppendLine("OOPS! The Server Manager has suffered from an internal error and must shut down, this is probably a bug and should be reported. The error files are:");
                message.AppendLine($"Error File: {file}");
                details.AppendLine();
                details.AppendLine();
                message.AppendLine($"Please report this crash to the Server Manager discord - {Config.Default.DiscordUrl}");
                message.AppendLine("The crash log will now be opened in notepad.");

                var result = MessageBox.Show(message.ToString(), "Server Manager crashed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.OK)
                {
                    Process.Start("notepad.exe", file);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(file, $"Exception trying to write exception: {ex.Message}\r\nStacktrace: {ex.StackTrace}");
                }
                catch { }
            }
        }
    }
}
