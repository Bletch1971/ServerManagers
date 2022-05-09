using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;

namespace ServerManagerTool.Updater
{
    public class Program
    {
        private static string[] ApplicationArgs
        {
            get;
            set;
        }

        private static string DownloadUrl
        {
            get;
            set;
        }

        private static string Prefix
        {
            get;
            set;
        }

        public static void Main(string[] args)
        {
            Console.Title = "Server Manager Updater";

            ServicePointManager.SecurityProtocol = SecurityUtils.GetSecurityProtocol(0xC00); // TLS12

            var updaterArgs = args;
            ApplicationArgs = null;
            DownloadUrl = null;
            Prefix = null;

            try
            {
                // check if the updater is already running
                if (ProcessUtils.IsAlreadyRunning())
                    throw new Exception("The updater is already running.");

                var process = Validate(updaterArgs);
                CloseApplication(process);
                process = null;

                Update();
                RestartApplication();

                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    OutputError(ex.InnerException.Message, null);
                }
                OutputError(ex.Message, ex.StackTrace);
                Environment.ExitCode = 1;

                OutputMessage("Press any key to continue.");
                Console.ReadKey(true);
            }
        }

        private static void CloseApplication(Process process)
        {
            OutputMessage("Closing application...");

            if (process == null || process.HasExited)
                throw new Exception("The process is not associated with a running application.");

            // send a close to the application
            process.CloseMainWindow();

            // wait until the application has been closed
            DateTime start = DateTime.Now;
            while (!process.HasExited)
            {
                if (start.AddMinutes(5) < DateTime.Now)
                {
                    process.Kill();
                    break;
                }
            }

            if (!process.HasExited)
                throw new Exception("The application could not be closed.");
        }

        private static void OutputError(string error, string stackTrace)
        {
            Console.WriteLine($"ERROR: {error}");
#if DEBUG
            if (!string.IsNullOrWhiteSpace(stackTrace))
                Console.WriteLine(stackTrace);
#endif
        }

        private static void OutputMessage(string message)
        {
            Console.WriteLine(message);
        }

        private static void RestartApplication()
        {
            OutputMessage("Restarting application...");

            var file = ApplicationArgs[0];
            var arguments = string.Empty;
            if (ApplicationArgs.Length > 1)
            {
                string[] argumentArray = new string[ApplicationArgs.Length - 1];
                Array.Copy(ApplicationArgs, 1, argumentArray, 0, argumentArray.Length);
                arguments = String.Join(" ", argumentArray);
            }

            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = file.AsQuoted(),
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = true,
            };

            var process = Process.Start(info);
            if (process == null)
                throw new Exception("Could not restart application.");
        }

        private static void Update()
        {
            if (ApplicationArgs == null || ApplicationArgs.Length == 0)
                throw new ArgumentNullException(nameof(ApplicationArgs), "The application args cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(DownloadUrl))
                throw new ArgumentNullException(nameof(DownloadUrl), "The download url cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(Prefix))
                throw new ArgumentNullException(nameof(Prefix), "The prefix cannot be null or empty.");

            OutputMessage("Starting upgrade process...");

            var currentInstallPath = Path.GetDirectoryName(ApplicationArgs[0]);
            var upgradeStagingPath = Path.GetTempPath();
            var applicationZip = Path.Combine(upgradeStagingPath, $"{Prefix}Latest.zip");
            var extractPath = Path.Combine(upgradeStagingPath, $"{Prefix}Latest");

            // Download the latest version
            using (var client = new WebClient())
            {
                OutputMessage("Downloading latest version file...");
#if DEBUG
                OutputMessage($"Url: {DownloadUrl}");
                OutputMessage($"Zip: {applicationZip}");
#endif
                client.DownloadFile(DownloadUrl, applicationZip);
            }

            // Unblock the downloaded file
            OutputMessage("Preparing downloaded file...");
#if DEBUG
            OutputMessage($"Zip: {applicationZip}");
#endif
            IOUtils.Unblock(applicationZip);

            try
            {
                // Delete the old extraction directory
                if (Directory.Exists(extractPath))
                {
#if DEBUG
                    OutputMessage("Deleting extract directory...");
                    OutputMessage($"Dir: {extractPath}");
#endif
                    Directory.Delete(extractPath, true);
                }
            }
            catch { }

            // Extract latest version to extraction directory
            OutputMessage("Extracting latest version to staging area...");
#if DEBUG
            OutputMessage($"Zip: {applicationZip}");
            OutputMessage($"Dir: {extractPath}");
#endif
            ZipFile.ExtractToDirectory(applicationZip, extractPath);

            // build a list of files to be updated
            var latestFiles = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories).ToList();

            // build a list of files to be removed
            var deleteFiles = Directory.GetFiles(currentInstallPath, "*.*", SearchOption.AllDirectories).ToList();
            foreach (var latestFile in latestFiles)
            {
                var file = Path.Combine(currentInstallPath, latestFile.Replace($"{extractPath}\\", ""));
                if (deleteFiles.Contains(file))
                    deleteFiles.Remove(file);
            }

            OutputMessage("Upgrading application...");

            // delete the obsolete files
            foreach (var deleteFile in deleteFiles)
            {
                if (File.Exists(deleteFile))
                {
#if DEBUG
                    OutputMessage($"Delete: {deleteFile}");
#endif
                    //File.Delete(deleteFile);
                }
            }

            // remove the updater from the files to be updated
            var assemblyFile = Assembly.GetEntryAssembly().Location;
            var assemblyPath = Path.GetDirectoryName(assemblyFile);

            var updaterFile = Path.Combine(extractPath, assemblyFile.Replace($"{assemblyPath}\\", ""));
#if DEBUG
                OutputMessage($"Checking: {updaterFile}");
#endif
            if (latestFiles.Contains(updaterFile))
                latestFiles.Remove(updaterFile);

            var updaterConfigFile = Path.Combine(updaterFile, ".config");
#if DEBUG
            OutputMessage($"Checking: {updaterConfigFile}");
#endif
            if (latestFiles.Contains(updaterConfigFile))
                latestFiles.Remove(updaterConfigFile);

            // Replace the current installation
            foreach (var latestFile in latestFiles)
            {
                var file = Path.Combine(currentInstallPath, latestFile.Replace($"{extractPath}\\", ""));
                if (File.Exists(latestFile))
                {
#if DEBUG
                    OutputMessage($"Copy: {latestFile} to {file}");
#endif
                    // check if the directory exists
                    var filePath = Path.GetDirectoryName(file);
                    if (!Directory.Exists(filePath))
                        Directory.CreateDirectory(filePath);

                    // copy over the file
                    File.Copy(latestFile, file, true);
                }
            }
        }

        private static Process Validate(string[] args)
        {
            OutputMessage("Validating update...");

            // argument format - PID, DownloadUrl, Prefix
            if (args == null)
                throw new ArgumentNullException(nameof(args), "The arguments cannot be null or empty");
            if (args == null || args.Length != 3)
                throw new ArgumentOutOfRangeException(nameof(args), "The arguments do not contain valid information.");

            // check if the passed pid is valid
            if (!int.TryParse(args[0], out int pid))
                throw new InvalidCastException("The process id value is not valid.");

            DownloadUrl = args[1];
            if (string.IsNullOrWhiteSpace(DownloadUrl))
                throw new ArgumentNullException(nameof(DownloadUrl), "The download url cannot be null or empty.");

            Prefix = args[2];
            if (string.IsNullOrWhiteSpace(Prefix))
                throw new ArgumentNullException(nameof(Prefix), "The prefix cannot be null or empty.");

            // get the process associated with the pid
            var process = ProcessUtils.GetProcess(pid);
            if (process == null)
                throw new Exception("The process id value is not associated with a running application.");

            // get a list of the running processes with the same name and file location
            var executablePath = ProcessUtils.GetMainModuleFilepath(pid);
            var processes = ProcessUtils.GetProcesses(process.ProcessName, executablePath);

            // check if there is more than one instance of the application running
            if (!processes.HasOne())
                throw new Exception("The application to be updated has more than one instance running.");

            // get the command line of the process
            var commandLine = ProcessUtils.GetCommandLineForProcess(pid);
            ApplicationArgs = ProcessUtils.CommandLineToArgs(commandLine);

            if (ApplicationArgs == null || ApplicationArgs.Length == 0)
                throw new ArgumentNullException(nameof(ApplicationArgs), "The application args cannot be null or empty.");

            return process;
        }
    }
}
