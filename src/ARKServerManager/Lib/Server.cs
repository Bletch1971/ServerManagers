using ServerManagerTool.Common.Lib;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ServerManagerTool.Lib
{
    public class Server : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(nameof(Profile), typeof(ServerProfile), typeof(Server), new PropertyMetadata((ServerProfile)null));
        public static readonly DependencyProperty RuntimeProperty = DependencyProperty.Register(nameof(Runtime), typeof(ServerRuntime), typeof(Server), new PropertyMetadata((ServerRuntime)null));
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(bool), typeof(Server), new PropertyMetadata(false));

        public ServerProfile Profile
        {
            get { return (ServerProfile)GetValue(ProfileProperty); }
            protected set { SetValue(ProfileProperty, value); }
        }

        public ServerRuntime Runtime
        {
            get { return (ServerRuntime)GetValue(RuntimeProperty); }
            protected set { SetValue(RuntimeProperty, value); }
        }

        public bool Selected
        {
            get { return (bool)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        private Server(ServerProfile profile)
        {
            InitializeFromProfile(profile);
        }

        public void Dispose()
        {
            this.Profile.DestroyServerFilesWatcher();

            this.Runtime.StatusUpdate -= Runtime_StatusUpdate;
            this.Runtime.Dispose();
        }

        private void Runtime_StatusUpdate(object sender, EventArgs eventArgs)
        {
            this.Profile.LastInstalledVersion = this.Runtime.Version.ToString();
        }

        public void ImportFromPath(string path, ServerProfile profile = null)
        {
            var loadedProfile = ServerProfile.LoadFrom(path, profile);
            if (loadedProfile != null)
                InitializeFromProfile(loadedProfile);
        }

        private void InitializeFromProfile(ServerProfile profile)
        {
            if (profile == null)
                return;

            this.Profile = profile;
            this.Runtime = new ServerRuntime();
            this.Runtime.AttachToProfile(this.Profile).Wait();

            this.Runtime.StatusUpdate += Runtime_StatusUpdate;
        }

        public static Server FromPath(string path)
        {
            var loadedProfile = ServerProfile.LoadFrom(path, profile: null);
            if (loadedProfile == null)
                return null;
            return new Server(loadedProfile);
        }

        public static Server FromDefaults()
        {
            var loadedProfile = ServerProfile.FromDefaults();
            if (loadedProfile == null)
                return null;
            return new Server(loadedProfile);
        }

        public async Task StartAsync()
        {
            await this.Runtime.AttachToProfile(this.Profile);
            await this.Runtime.StartAsync();
        }

        public async Task StopAsync()
        {
            await this.Runtime.StopAsync();
        }

        public async Task<bool> UpgradeAsync(CancellationToken cancellationToken, bool updateServer, BranchSnapshot branch, bool validate, bool updateMods, ProgressDelegate progressCallback)
        {
            await this.Runtime.AttachToProfile(this.Profile);
            var success = await this.Runtime.UpgradeAsync(cancellationToken, updateServer, branch, validate, updateMods, progressCallback);
            this.Profile.LastInstalledVersion = this.Runtime.Version.ToString();
            return success;
        }

        public async Task ResetAsync()
        {
            // delete the world, player and tribe files (SavedArks)
            var saveFolder = ServerProfile.GetProfileSavePath(Profile);
            if (Directory.Exists(saveFolder))
            {
                await Task.Run(() =>
                {
                    foreach (var file in Directory.GetFiles(saveFolder, "*.*", SearchOption.TopDirectoryOnly))
                    {
                        File.Delete(file);
                    }
                });
            };

            // delete the mod files (SaveGames)
            var saveGamesFolder = ServerProfile.GetProfileSaveGamesPath(Profile);
            if (Directory.Exists(saveGamesFolder))
            {
                await Task.Run(() =>
                {
                    Directory.Delete(saveGamesFolder, true);
                });
            }

            // delete the log files (Logs)
            var logsFolder = Path.Combine(Profile.InstallDirectory, Config.Default.SavedRelativePath, Config.Default.LogsDir);
            if (Directory.Exists(logsFolder))
            {
                await Task.Run(() =>
                {
                    Directory.Delete(logsFolder, true);
                });
            }
        }
    }
}
