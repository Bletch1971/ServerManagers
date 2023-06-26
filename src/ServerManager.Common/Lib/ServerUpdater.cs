using ServerManagerTool.Common.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ServerManagerTool.Common.Lib
{
    public static class ServerUpdater
    {
        public static Task<bool> UpgradeServerAsync(string steamCmdFile, string steamCmdArgs, string workingDirectory, string username, SecureString password, string serverInstallDirectory, List<int> SteamCmdIgnoreExitStatusCodes, DataReceivedEventHandler outputHandler, CancellationToken cancellationToken, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
        {
            Directory.CreateDirectory(serverInstallDirectory);

            return ProcessUtils.RunProcessAsync(steamCmdFile, steamCmdArgs, string.Empty, workingDirectory, username, password, SteamCmdIgnoreExitStatusCodes, outputHandler, cancellationToken, windowStyle);
        }

        public static Task<bool> UpgradeModsAsync(string steamCmdFile, string steamCmdArgs, string workingDirectory, string username, SecureString password, List<int> SteamCmdIgnoreExitStatusCodes, DataReceivedEventHandler outputHandler, CancellationToken cancellationToken, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
        {
            return ProcessUtils.RunProcessAsync(steamCmdFile, steamCmdArgs, string.Empty, workingDirectory, username, password, SteamCmdIgnoreExitStatusCodes, outputHandler, cancellationToken, windowStyle);
        }
    }
}
