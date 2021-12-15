using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace ServerManagerTool.Updater
{
    public static class ProcessUtils
    {
        public static string FIELD_COMMANDLINE = "CommandLine";
        public static string FIELD_EXECUTABLEPATH = "ExecutablePath";
        public static string FIELD_PROCESSID = "ProcessId";

        private static Mutex _mutex;

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            var argv = CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
                throw new Win32Exception();

            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        public static string GetCommandLineForProcess(int processId)
        {
            var wmiQueryString = $"SELECT {FIELD_COMMANDLINE} FROM Win32_Process WHERE {FIELD_PROCESSID} = {processId}";

            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            {
                using (var results = searcher.Get())
                {
                    ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
                    if (mo != null)
                        return (string)mo[FIELD_COMMANDLINE];
                }
            }

            return null;
        }

        public static string GetMainModuleFilepath(int processId)
        {
            var wmiQueryString = $"SELECT {FIELD_EXECUTABLEPATH} FROM Win32_Process WHERE {FIELD_PROCESSID} = {processId}";

            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            {
                using (var results = searcher.Get())
                {
                    ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
                    if (mo != null)
                        return (string)mo[FIELD_EXECUTABLEPATH];
                }
            }

            return null;
        }

        public static Process GetProcess(int processId)
        {
            return Process.GetProcessById(processId);
        }

        public static IEnumerable<Process> GetProcesses(string processName, string executablePath)
        {
            var runningProcesses = Process.GetProcessesByName(processName).ToList();

            for (var i = runningProcesses.Count - 1; i >= 0; i--)
            {
                var process = runningProcesses[i];
                var runningPath = GetMainModuleFilepath(process.Id);
                if (!string.Equals(executablePath, runningPath, StringComparison.OrdinalIgnoreCase))
                    runningProcesses.RemoveAt(i);
            }

            return runningProcesses;
        }

        public static bool IsAlreadyRunning()
        {
            var assemblyLocation = Assembly.GetEntryAssembly().Location;
            var name = $"Global::{Path.GetFileName(assemblyLocation)}";

            _mutex = new Mutex(true, name, out bool createdNew);
            if (createdNew)
                _mutex.ReleaseMutex();

            return !createdNew;
        }
    }
}
