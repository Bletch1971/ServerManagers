using ServerManagerTool.Plugin.Common.Delegates;
using ServerManagerTool.Plugin.Common.Lib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ServerManagerTool.Plugin.Common
{
    public sealed class PluginHelper : IDisposable
    {
        private const string PLUGINFILE_FOLDER = "Plugins";
        private const string PLUGINFILE_EXTENSION = "dll";
        public const string LANGUAGECODE_FALLBACK = "en-US";

        private static volatile PluginHelper _instance;
        private static readonly object _syncLock = new object();

        private readonly Object _syncLockProcessAlert = new Object();
        private readonly Object _syncLockFetchProfiles = new Object();
        private FetchProfilesDelegate _fetchProfilesCallback;
        private bool _disposed;

        private PluginHelper()
        {
            BetaEnabled = false;
            LanguageCode = LANGUAGECODE_FALLBACK;
            Plugins = new ObservableCollection<PluginItem>();
        }

        public static PluginHelper Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock(_syncLock)
                {
                    if (_instance == null)
                        _instance = new PluginHelper();
                }
                return _instance;
            }
        }

        public static string PluginFolder
        {
            get
            {
                var folder = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Environment.CurrentDirectory);
                return Path.Combine(folder, PLUGINFILE_FOLDER);
            }
        }

        internal bool BetaEnabled
        {
            get;
            set;
        }

        public string LanguageCode
        {
            get;
            set;
        }

        public ObservableCollection<PluginItem> Plugins
        {
            get;
            private set;
        }

        internal void AddPlugin(string folder, string pluginFile)
        {
            if (!CheckPluginFile(pluginFile))
                throw new PluginException("The selected file does not contain server manager plugins or is for a previous version of server manager.");

            var pluginFolder = Path.Combine(folder, PLUGINFILE_FOLDER);
            if (!Directory.Exists(pluginFolder))
                Directory.CreateDirectory(pluginFolder);

            var newPluginFile = Path.Combine(pluginFolder, $"{Path.GetFileName(pluginFile)}");
            if (File.Exists(newPluginFile))
                throw new PluginException("A file with the same name already exists, delete the existing file and try again.");

            File.Copy(pluginFile, newPluginFile, true);

            LoadPlugin(newPluginFile);
        }

        internal bool CheckPluginFile(string pluginFile)
        {
            if (string.IsNullOrWhiteSpace(pluginFile))
                return false;
            if (!File.Exists(pluginFile))
                return false;

            Assembly assembly = Assembly.Load(File.ReadAllBytes(pluginFile));
            if (assembly == null)
                return false;

            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                return false;
            }

            if (types.Length == 0)
                return false;

            // check if the file contains a plugin
            foreach (Type type in types)
            {
                if (type.GetInterface(typeof(IPlugin).Name) != null)
                    return true;
            }

            return false;
        }

        internal void DeleteAllPlugins()
        {
            for (int index = Plugins.Count - 1; index >= 0; index--)
            {
                var pluginFile = Plugins[index].PluginFile;

                Plugins.RemoveAt(index);

                if (File.Exists(pluginFile))
                    File.Delete(pluginFile);
            }
        }

        internal void DeletePlugin(string pluginFile)
        {
            if (string.IsNullOrWhiteSpace(pluginFile))
                return;

            for (int index = Plugins.Count - 1; index >= 0; index--)
            {
                if (Plugins[index].PluginFile.Equals(pluginFile, StringComparison.OrdinalIgnoreCase))
                    Plugins.RemoveAt(index);
            }

            if (File.Exists(pluginFile))
                File.Delete(pluginFile);
        }

        public IList<Profile> FetchProfileList()
        {
            lock (_syncLockFetchProfiles)
            {
                return _fetchProfilesCallback?.Invoke() ?? new List<Profile>();
            }
        }

        internal void LoadPlugin(string pluginFile)
        {
            if (string.IsNullOrWhiteSpace(pluginFile))
                return;
            if (!File.Exists(pluginFile))
                return;

            Assembly assembly = Assembly.Load(File.ReadAllBytes(pluginFile));
            if (assembly == null)
                return;

            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                return;
            }

            if (types.Length == 0)
                return;

            // check if the file contains one or more plugins
            foreach (Type type in types)
            {
                try
                {
                    if (type.GetInterface(typeof(IAlertPlugin).Name) != null)
                    {
                        var plugin = assembly.CreateInstance(type.FullName) as IAlertPlugin;
                        if (plugin != null && plugin.Enabled)
                        {
                            if (type.GetInterface(typeof(IBeta).Name) != null)
                                ((IBeta)plugin).BetaEnabled = BetaEnabled;
                            plugin.Initialize();

                            Plugins.Add(new PluginItem { Plugin = plugin, PluginFile = pluginFile, PluginType = nameof(IAlertPlugin) });
                        }
                    }
                    else if (type.GetInterface(typeof(IPlugin).Name) != null)
                    {
                        var plugin = assembly.CreateInstance(type.FullName) as IPlugin;
                        if (plugin != null && plugin.Enabled)
                        {
                            if (type.GetInterface(typeof(IBeta).Name) != null)
                                ((IBeta)plugin).BetaEnabled = BetaEnabled;
                            plugin.Initialize();

                            Plugins.Add(new PluginItem { Plugin = plugin, PluginFile = pluginFile, PluginType = nameof(IPlugin) });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR: {nameof(LoadPlugin)} - {type.FullName}\r\n{ex.Message}");
                }
            }
        }

        internal void LoadPlugins(string folder, bool ClearExisting)
        {
            if (ClearExisting)
                Plugins.Clear();

            var pluginFolder = Path.Combine(folder, PLUGINFILE_FOLDER);
            if (string.IsNullOrWhiteSpace(pluginFolder))
                return;
            if (!Directory.Exists(pluginFolder))
                return;

            var pluginFiles = Directory.GetFiles(pluginFolder, $"*.{PLUGINFILE_EXTENSION}");
            foreach (var pluginFile in pluginFiles)
            {
                LoadPlugin(pluginFile);
            }
        }

        public void OnResourceDictionaryChanged(string languageCode)
        {
            LanguageCode = languageCode;
        }

        internal void OpenConfigForm(string pluginCode, Window owner)
        {
            if (Plugins == null)
                return;

            var pluginItem = Plugins.FirstOrDefault(p => p.Plugin.PluginCode.Equals(pluginCode, StringComparison.OrdinalIgnoreCase));
            OpenConfigForm(pluginItem.Plugin, owner);
        }

        internal void OpenConfigForm(IPlugin plugin, Window owner)
        {
            if (plugin == null || !plugin.Enabled || !plugin.HasConfigForm)
                return;

            plugin.OpenConfigForm(owner);
        }

        internal bool ProcessAlert(AlertType alertType, string profileName, string alertMessage)
        {
            if (Plugins == null || Plugins.Count == 0 || string.IsNullOrWhiteSpace(alertMessage))
                return false;

            var plugins = Plugins.Where(p => (p.PluginType is nameof(IAlertPlugin)) && (p.Plugin?.Enabled ?? false));
            if (plugins.IsEmpty())
                return false;

            lock (_syncLockProcessAlert)
            {
                var message = alertMessage.Replace("\\r\\n", "\\n");
                message = message.Replace("\\n", "\n");

                foreach (var pluginItem in plugins)
                {
                    ((IAlertPlugin)pluginItem.Plugin).HandleAlert(alertType, profileName, message.ToString());
                }
            }

            return true;
        }

        internal void SetFetchProfileCallback(FetchProfilesDelegate callback)
        {
            _fetchProfilesCallback = callback;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _fetchProfilesCallback = null;
                _instance = null;
            }

            _disposed = true;
        }
    }
}
