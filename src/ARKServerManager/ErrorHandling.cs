using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib;

namespace ServerManagerTool
{
    public static class ErrorHandling
    {
        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Oops!  Bad news everyone - the app is going down!
            // Write out a log file with all the details so users can send us the info...
            
            var file = Path.GetTempFileName();
            var crashFile = file + ".dmp";

            try
            {
                MiniDumpToFile(crashFile);
            }
            catch { }

            try
            {
                var details = new StringBuilder();
                details.AppendLine("ARK Server Manager Crash Report");
                details.AppendLine($"Please report this crash to the Server Manager discord - {Config.Default.DiscordUrl}");
                details.AppendLine();

                details.AppendLine($"Assembly: {Assembly.GetExecutingAssembly()}");
                details.AppendLine($"Version: {App.Instance.Version}");
                details.AppendLine($"IsAdministrator: {SecurityUtils.IsAdministrator()}");
                details.AppendLine();

                details.AppendLine($"Windows Platform: {Environment.OSVersion.Platform}");
                details.AppendLine($"Windows Version: {Environment.OSVersion.VersionString}");
                details.AppendLine();

                details.AppendLine($"Crash Dump: {crashFile}");
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
                message.AppendLine("OOPS! ARK Server Manager has suffered from an internal error and must shut down, this is probably a bug and should be reported. The error files are:");
                message.AppendLine($"Error File: {file}");
                message.AppendLine($"Crash Dump: {crashFile}");
                details.AppendLine();
                details.AppendLine();
                message.AppendLine($"Please report this crash to the Server Manager discord - {Config.Default.DiscordUrl}");
                message.AppendLine("The crash log will now be opened in notepad.");

                var result = MessageBox.Show(message.ToString(), "ARK Server Manager crashed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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

        internal enum MinidumpType
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }

        [DllImport("dbghelp.dll")]
        static extern bool MiniDumpWriteDump(IntPtr hProcess, Int32 ProcessId, IntPtr hFile, MinidumpType DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallackParam);

        public static void MiniDumpToFile(String fileToDump)
        {
            var fsToDump = File.Create(fileToDump);

            Process thisProcess = Process.GetCurrentProcess();
            MiniDumpWriteDump(thisProcess.Handle, thisProcess.Id, fsToDump.SafeFileHandle.DangerousGetHandle(), MinidumpType.MiniDumpWithFullMemory, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            fsToDump.Close();
        }
    }
}
