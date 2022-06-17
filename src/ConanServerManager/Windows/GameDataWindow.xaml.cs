using Microsoft.WindowsAPICodePack.Dialogs;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib;
using ServerManagerTool.Lib.ViewModel;
using ServerManagerTool.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for GameDataWindow.xaml
    /// </summary>
    public partial class GameDataWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty GameDataFilesProperty = DependencyProperty.Register(nameof(GameDataFiles), typeof(GameDataFileList), typeof(GameDataWindow), new PropertyMetadata(null));

        public GameDataWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.GameDataFiles = new GameDataFileList();

            this.DataContext = this;
        }

        public GameDataFileList GameDataFiles
        {
            get { return GetValue(GameDataFilesProperty) as GameDataFileList; }
            set { SetValue(GameDataFilesProperty, value); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ReloadGameDataFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("GameDataWindow_LoadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddGameData_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Title = GlobalizedApplication.Instance.GetResourceString("GameDataWindow_AddDialogTitle");
            dialog.DefaultExtension = GlobalizedApplication.Instance.GetResourceString("GameDataWindow_GameDataDefaultExtension");
            dialog.Filters.Add(new CommonFileDialogFilter(GlobalizedApplication.Instance.GetResourceString("GameDataWindow_AddFilterLabel"), GlobalizedApplication.Instance.GetResourceString("GameDataWindow_AddFilterExtension")));
            if (dialog == null || dialog.ShowDialog(this) != CommonFileDialogResult.Ok)
                return;

            try
            {
                AddGameDataFile(GameData.UserDataFolder, dialog.FileName);
                GameData.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("GameDataWindow_AddErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearGameData_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("GameDataWindow_ClearLabel"), _globalizer.GetResourceString("GameDataWindow_ClearTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (!GameData.UserDataFolder.Equals(GameData.MainDataFolder, StringComparison.OrdinalIgnoreCase))
                {
                    DeleteAllGameDataFiles(GameData.UserDataFolder);
                }
                GameData.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("GameDataWindow_ClearErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenGameDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(GameData.UserDataFolder))
                    Directory.CreateDirectory(GameData.UserDataFolder);
                Process.Start("explorer.exe", GameData.UserDataFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("GameDataWindow_OpenErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GameDataForum_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.Default.GameDataUrl);
        }

        private void ReloadGameData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReloadGameDataFiles();
                GameData.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("GameDataWindow_LoadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveGameData_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_globalizer.GetResourceString("GameDataWindow_DeleteLabel"), _globalizer.GetResourceString("GameDataWindow_DeleteTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var gameDataItem = ((GameDataFile)((Button)e.Source).DataContext);
                DeleteGameDataFile(gameDataItem.File, true);
                GameData.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("GameDataWindow_DeleteErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateGameData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ValidateAllGameDataFiles(GameData.UserDataFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("GameDataWindow_ClearErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddGameDataFile(string folder, string gameDataFile)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var newGameDataFile = Path.Combine(folder, $"{Path.GetFileName(gameDataFile)}");
            if (File.Exists(newGameDataFile))
                throw new Exception(_globalizer.GetResourceString("GameDataWindow_ExistingFileErrorLabel"));

            ValidateGameDataFile(gameDataFile);

            File.Copy(gameDataFile, newGameDataFile, true);

            LoadGameDataFile(newGameDataFile, true);
        }

        private void DeleteAllGameDataFiles(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return;

            var fileList = Directory.GetFiles(folder, $"*.{GlobalizedApplication.Instance.GetResourceString("GameDataWindow_GameDataDefaultExtension")}");
            foreach (var file in fileList)
            {
                DeleteGameDataFile(file, false);
            }

            LoadGameDataFiles(GameData.UserDataFolder, true, true);
            LoadGameDataFiles(GameData.MainDataFolder, false, false);
        }

        private void DeleteGameDataFile(string file, bool updateList)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return;

            File.Delete(file);

            if (updateList)
            {
                var gameDataFiles = GameDataFiles.Where(f => f.File == file).ToList();
                foreach (var gameDataFile in gameDataFiles)
                {
                    GameDataFiles.Remove(gameDataFile);
                }
            }
        }

        private void LoadGameDataFile(string file, bool isUserData)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return;

            BaseGameData baseGameData = null;

            try
            {
                baseGameData = BaseGameData.Load(file);
            }
            catch
            {
                // do nothing, just swallow the error
            }

            var gameDataFile = new GameDataFile
            {
                CreatedDate = baseGameData?.Created ?? DateTime.MinValue,
                File = file,
                FileName = string.IsNullOrWhiteSpace(file) ? string.Empty : Path.GetFileNameWithoutExtension(file),
                IsUserData = isUserData,
                Version = baseGameData?.Version ?? "0.0.0",
                HasError = baseGameData == null,
            };
            GameDataFiles.Add(gameDataFile);
        }

        private void LoadGameDataFiles(string folder, bool isUserData, bool ClearExisting)
        {
            if (ClearExisting)
                GameDataFiles.Clear();

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return;

            var files = Directory.GetFiles(folder, $"*.{GlobalizedApplication.Instance.GetResourceString("GameDataWindow_GameDataDefaultExtension")}");
            foreach (var file in files)
            {
                LoadGameDataFile(file, isUserData);
            }
        }

        private void ReloadGameDataFiles()
        {
            if (!GameData.UserDataFolder.Equals(GameData.MainDataFolder, StringComparison.OrdinalIgnoreCase))
            {
                LoadGameDataFiles(GameData.UserDataFolder, true, true);
            }
            LoadGameDataFiles(GameData.MainDataFolder, false, false);
        }

        private void ValidateAllGameDataFiles(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show(_globalizer.GetResourceString("GameDataWindow_ValidateSuccessLabel"), _globalizer.GetResourceString("GameDataWindow_ValidateSuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var fileList = Directory.GetFiles(folder, $"*.{GlobalizedApplication.Instance.GetResourceString("GameDataWindow_GameDataDefaultExtension")}");
            var errorList = new List<string>();

            foreach (var file in fileList)
            {
                try
                {
                    ValidateGameDataFile(file);
                }
                catch (Exception ex)
                {
                    errorList.Add($"{Path.GetFileNameWithoutExtension(file)} - {ex.Message}");
                }
            }

            if (errorList.Count > 0)
            {
                var message = $"{_globalizer.GetResourceString("GameDataWindow_ValidateErrorLabel")}{Environment.NewLine}{string.Join(Environment.NewLine, errorList)}";
                var window = new CommandLineWindow(message);
                window.OutputTextWrapping = TextWrapping.NoWrap;
                window.Height = 300;
                window.Width = 600;
                window.Title = _globalizer.GetResourceString("GameDataWindow_ValidateErrorTitle");
                window.Owner = this;
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show(_globalizer.GetResourceString("GameDataWindow_ValidateSuccessLabel"), _globalizer.GetResourceString("GameDataWindow_ValidateSuccessTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ValidateGameDataFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                return;

            MainGameData gameData = null;

            try
            {
                gameData = MainGameData.Load(file, true);
            }
            catch (Exception ex)
            {
                var message = _globalizer.GetResourceString("GameDataWindow_ValidateErrorMessage");
                throw new Exception(message, ex);
            }

            if (gameData == null)
            {
                var message = _globalizer.GetResourceString("GameDataWindow_ValidateErrorMessage");
                throw new Exception(message);
            }
        }
    }
}
