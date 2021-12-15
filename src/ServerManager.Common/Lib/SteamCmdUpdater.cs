using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.Common.Lib
{
    public delegate void ProgressDelegate(int progress, string message, bool newLine = true);

    public class SteamCmdUpdater
    {
        public const string OUTPUT_PREFIX = "[UPDATER]";

        public struct Update
        {
            public Update(string statusKey, float completionPercent)
            {
                this.StatusKey = statusKey;
                this.CompletionPercent = completionPercent;
                this.Cancelled = false;
                this.Failure = null;
                this.FailureText = null;
            }

            public static Update AsCompleted(string statusKey)
            {
                return new Update { StatusKey = statusKey, CompletionPercent = 100, Cancelled = false };
            }

            public static Update AsCancelled(string statusKey)
            {
                return new Update { StatusKey = statusKey, CompletionPercent = 100, Cancelled = true };
            }

            public Update SetFailed(Exception ex)
            {
                this.Failure = ex;
                this.FailureText = ex.Message;
                return this;
            }

            public string StatusKey;
            public float CompletionPercent;
            public bool Cancelled;
            public Exception Failure;
            public string FailureText;
        }

        enum Status
        {
            CleaningSteamCmd,
            DownloadingSteamCmd,
            UnzippingSteamCmd,
            RunningSteamCmd,
            InstallSteamCmdComplete,
            Complete,
            Cancelled,
            Failed,
        }

        readonly Dictionary<Status, Update> statuses = new Dictionary<Status, Update>()
        {
           { Status.CleaningSteamCmd, new Update("AutoUpdater_Status_CleaningSteamCmd", 0) },
           { Status.DownloadingSteamCmd, new Update("AutoUpdater_Status_DownloadingSteamCmd", 10) },
           { Status.UnzippingSteamCmd, new Update("AutoUpdater_Status_UnzippingSteamCmd", 30) },
           { Status.RunningSteamCmd, new Update("AutoUpdater_Status_RunningSteamCmd", 50) },
           { Status.InstallSteamCmdComplete, new Update("AutoUpdater_Status_InstallSteamCmdComplete", 80) },
           { Status.Complete, Update.AsCompleted("AutoUpdater_Status_Complete") },
           { Status.Cancelled, Update.AsCancelled("AutoUpdater_Status_Cancelled") },
           { Status.Failed, Update.AsCancelled("AutoUpdater_Status_Failed") },
        };

        public static string GetSteamCmdFile(string dataPath) => IOUtils.NormalizePath(Path.Combine(dataPath, CommonConfig.Default.SteamCmdRelativePath, CommonConfig.Default.SteamCmdExeFile));

        public async Task ReinstallSteamCmdAsync(string dataPath, IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dataPath))
                return;

            try
            {
                reporter?.Report(statuses[Status.CleaningSteamCmd]);

                string steamCmdDirectory = Path.Combine(dataPath, CommonConfig.Default.SteamCmdRelativePath);
                if (Directory.Exists(steamCmdDirectory))
                {
                    Directory.Delete(steamCmdDirectory, true);
                }

                await Task.Delay(5000);
                await InstallSteamCmdAsync(dataPath, reporter, cancellationToken);

                reporter?.Report(statuses[Status.InstallSteamCmdComplete]);
                reporter?.Report(statuses[Status.Complete]);
            }
            catch (TaskCanceledException)
            {
                reporter?.Report(statuses[Status.Cancelled]);
            }
            catch (Exception ex)
            {
                reporter?.Report(statuses[Status.Failed].SetFailed(ex));
            }
        }

        private async Task InstallSteamCmdAsync(string dataPath, IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dataPath))
                return;

            string steamCmdDirectory = Path.Combine(dataPath, CommonConfig.Default.SteamCmdRelativePath);
            if (!Directory.Exists(steamCmdDirectory))
            {
                Directory.CreateDirectory(steamCmdDirectory);
            }

            reporter?.Report(statuses[Status.DownloadingSteamCmd]);

            // Get SteamCmd.exe if necessary
            string steamCmdPath = Path.Combine(steamCmdDirectory, CommonConfig.Default.SteamCmdExeFile);
            if (!File.Exists(steamCmdPath))
            {
                // download the SteamCMD zip file
                var steamZipPath = Path.Combine(steamCmdDirectory, CommonConfig.Default.SteamCmdZipFile);
                using (var webClient = new WebClient())
                {
                    using (var cancelRegistration = cancellationToken.Register(webClient.CancelAsync))
                    {
                        await webClient.DownloadFileTaskAsync(CommonConfig.Default.SteamCmdUrl, steamZipPath);
                    }
                }

                // Unzip the downloaded file
                reporter?.Report(statuses[Status.UnzippingSteamCmd]);

                ZipFile.ExtractToDirectory(steamZipPath, steamCmdDirectory);
                File.Delete(steamZipPath);

                // Run the SteamCmd updater
                reporter?.Report(statuses[Status.RunningSteamCmd]);

                var arguments = SteamUtils.BuildSteamCmdArguments(false, CommonConfig.Default.SteamCmdInstallArgs);
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = steamCmdPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                };

                var process = Process.Start(startInfo);
                process.EnableRaisingEvents = true;

                var ts = new TaskCompletionSource<bool>();
                using (var cancelRegistration = cancellationToken.Register(() => 
                    {
                        try
                        {
                            process.Kill();
                        }
                        finally
                        {
                            ts.TrySetCanceled();
                        }
                    }))
                {
                    process.Exited += (s, e) => 
                    {
                        ts.TrySetResult(process.ExitCode == 0);
                    };
                    await ts.Task;
                }
            }

            return;
        }

        public async void UpdateSteamCmdAsync(string dataPath, IProgress<Update> reporter, CancellationToken cancellationToken)
        {
            try
            {
                await InstallSteamCmdAsync(dataPath, reporter, cancellationToken);

                reporter?.Report(statuses[Status.InstallSteamCmdComplete]);
                reporter?.Report(statuses[Status.Complete]);
            }
            catch (TaskCanceledException)
            {
                reporter?.Report(statuses[Status.Cancelled]);
            }
            catch(Exception ex)
            {
                reporter?.Report(statuses[Status.Failed].SetFailed(ex));
            }
        }
    }
}
