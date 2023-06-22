using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.Common.Utils
{
    public static class ProcessUtils
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        // Delegate type to be used as the Handler Routine for SCCH
        delegate bool ConsoleCtrlDelegate(CtrlTypes CtrlType);

        // Enumerated type for the control messages sent to the handler routine
        enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int IsIconic(IntPtr hWnd);

        public static string FIELD_COMMANDLINE = "CommandLine";
        public static string FIELD_EXECUTABLEPATH = "ExecutablePath";
        public static string FIELD_PROCESSID = "ProcessId";

        private const int SW_RESTORE = 9;

        private static Mutex _mutex;

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

        public static async Task SendStopAsync(Process process)
        {
            if (process == null)
                return;

            var ts = new TaskCompletionSource<bool>();
            EventHandler handler = (s, e) => ts.TrySetResult(true);

            try
            {
                process.Exited += handler;

                //This does not require the console window to be visible.
                var result = AttachConsole((uint)process.Id);
                if (result)
                {
                    // Disable Ctrl-C handling for our program
                    result = SetConsoleCtrlHandler(null, true);
                    if (result)
                    {
                        result = GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);
                        if (result)
                        {
                            FreeConsole();

                            // Must wait here. If we don't and re-enable Ctrl-C
                            // handling below too fast, we might terminate ourselves.
                            try
                            {
                                ts.Task.Wait(10000);
                                result = true;
                            }
                            catch (Exception)
                            {
                                result = false;
                            }

                            //Re-enable Ctrl-C handling or any subsequently started
                            //programs will inherit the disabled state.
                            SetConsoleCtrlHandler(null, false);
                        }
                        else
                        {
                            //Re-enable Ctrl-C handling or any subsequently started
                            //programs will inherit the disabled state.
                            SetConsoleCtrlHandler(null, false);

                            FreeConsole();
                        }
                    }
                    else
                    {
                        FreeConsole();
                    }
                }
                
                if (!result && !process.HasExited)
                {
                    process.Kill();
                }
            }
            finally
            {
                process.Exited -= handler;
            }
        }

        public static Task<bool> RunProcessAsync(string file, string arguments, string verb, string workingDirectory, string username, SecureString password, List<int> SteamCmdIgnoreExitStatusCodes, DataReceivedEventHandler outputHandler, CancellationToken cancellationToken, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                    throw new FileNotFoundException("The specified file does not exist or could not be found.", file);

                var fileName = Path.GetFileName(file);

                var startInfo = new ProcessStartInfo()
                {
                    FileName = file,
                    Arguments = arguments,
                    Verb = verb,
                    UseShellExecute = outputHandler == null && windowStyle == ProcessWindowStyle.Minimized && string.IsNullOrWhiteSpace(username),
                    RedirectStandardOutput = outputHandler != null,
                    CreateNoWindow = outputHandler != null || windowStyle == ProcessWindowStyle.Hidden,
                    WindowStyle = windowStyle,
                    UserName = string.IsNullOrWhiteSpace(username) ? null : username,
                    Password = string.IsNullOrWhiteSpace(username) ? null : password,
                    WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory) ? Path.GetDirectoryName(file) : workingDirectory,
                };

                var process = Process.Start(startInfo);
                process.EnableRaisingEvents = true;
                if (startInfo.RedirectStandardOutput && outputHandler != null)
                {
                    process.OutputDataReceived += outputHandler;
                    process.BeginOutputReadLine();
                }

                var tcs = new TaskCompletionSource<bool>();
                using (var cancelRegistration = cancellationToken.Register(() =>
                    {
                        try
                        {
                            process.Kill();
                        }
                        finally
                        {
                            tcs.TrySetCanceled();
                        }
                    }))
                {
                    process.Exited += (s, e) =>
                    {
                        var exitCode = process.ExitCode;

                        _logger.Debug($"{nameof(RunProcessAsync)}: filename {fileName}; exitcode = {exitCode}");

                        if (exitCode != 0)
                        {
                            _logger.Error($"{nameof(RunProcessAsync)}: filename {fileName}; exitcode = {exitCode}");

                            if (SteamCmdIgnoreExitStatusCodes.Contains(exitCode))
                            {
                                exitCode = 0;
                            }
                        }

                        tcs.TrySetResult(exitCode == 0);
                        process.Close();
                    };
                    return tcs.Task;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(RunProcessAsync)}. {ex.Message}\r\n{ex.StackTrace}");
                throw;
            }
        }

        private static IntPtr GetCurrentInstanceWindowHandle()
        {
            var hWnd = IntPtr.Zero;
            var currentProcess = Process.GetCurrentProcess();

            var processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (var process in processes)
            {
                // Get the first instance that is not this instance, has the same process name and was started from the same file name
                // and location. Also check that the process has a valid window handle in this session to filter out other user's processes.
                if (process.Id != currentProcess.Id && process.MainModule.FileName == currentProcess.MainModule.FileName && process.MainWindowHandle != IntPtr.Zero)
                {
                    hWnd = process.MainWindowHandle;
                    break;
                }
            }

            return hWnd;
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

        public static bool SwitchToCurrentInstance()
        {
            var hWnd = GetCurrentInstanceWindowHandle();
            if (hWnd == IntPtr.Zero)
                return false;

            // Restore window if minimised. Do not restore if already in normal or maximised window state, since we don't want to
            // change the current state of the window.
            if (IsIconic(hWnd) != 0)
                ShowWindow(hWnd, SW_RESTORE);

            // Set foreground window.
            SetForegroundWindow(hWnd);

            return true;
        }

        public static int ProcessorCount
        {
            get
            {
                var processorCount = Environment.ProcessorCount;
//#if DEBUG
//                processorCount = 40;
//#endif
                return processorCount;
            }
        }

        public static IEnumerable<BigInteger> GetProcessorAffinityList()
        {
            var processorCount = ProcessorCount;
            var results = new List<BigInteger>(processorCount + 1);
            results.Add(BigInteger.Zero); // default - all processors

            for (int index = 0; index < processorCount; index++)
            {
                results.Add((BigInteger)Math.Pow(2, index));
            }
            return results;
        }

        public static string[] GetProcessPriorityList()
        {
            return new string[] { "low", "belownormal", "normal", "abovenormal", "high" };
        }

        public static bool IsProcessorAffinityValid(BigInteger affinityValue)
        {
            if (affinityValue == BigInteger.Zero)
                return true;

            var maxaffinity = (BigInteger)Math.Pow(2, ProcessorCount);
            if (affinityValue < BigInteger.Zero || affinityValue > maxaffinity)
                return false;

            return true;
        }

        public static bool IsProcessPriorityValid(string priorityValue)
        {
            if (string.IsNullOrWhiteSpace(priorityValue))
                return false;

            var priorityList = GetProcessPriorityList();
            if (!priorityList.Contains(priorityValue))
                return false;

            return true;
        }
    }
}
