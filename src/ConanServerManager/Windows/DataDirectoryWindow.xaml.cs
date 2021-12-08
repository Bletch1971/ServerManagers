using ServerManagerTool.Common;
using ServerManagerTool.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Windows
{
    /// <summary>
    /// Interaction logic for DriveSelectionWindow.xaml
    /// </summary>
    public partial class DataDirectoryWindow : Window
    {
        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public static readonly DependencyProperty DriveInformationProperty = DependencyProperty.Register(nameof(DriveInformation), typeof(List<DriveInfoDisplay>), typeof(DataDirectoryWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty FolderNameProperty = DependencyProperty.Register(nameof(FolderName), typeof(string), typeof(DataDirectoryWindow), new PropertyMetadata(null));

        public DataDirectoryWindow()
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            PopulateDriveInformation();
        }

        public List<DriveInfoDisplay> DriveInformation
        {
            get { return (List<DriveInfoDisplay>)GetValue(DriveInformationProperty); }
            set { SetValue(DriveInformationProperty, value); }
        }

        public string FolderName
        {
            get { return (string)GetValue(FolderNameProperty); }
            set { SetValue(FolderNameProperty, value); }
        }

        private void PopulateDriveInformation()
        {
            this.FolderName = Config.Default.DefaultDataDirectoryName;
            this.DriveInformation = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed).Select(d => new DriveInfoDisplay(d)).ToList();

            var installationFolder = Path.GetPathRoot(Assembly.GetEntryAssembly().Location);
            if (!installationFolder.EndsWith(@"\"))
                installationFolder += @"\";

            foreach (var driveInfo in DriveInformation)
            {
                if (driveInfo.DriveInfo.RootDirectory.FullName.Equals(installationFolder))
                {
                    this.DriveSelectionListBox.SelectedItem = driveInfo;
                    break;
                }
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = CreateDataDirectory();
                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show(_globalizer.GetResourceString("DataDirectory_RestartLabel"), _globalizer.GetResourceString("DataDirectory_RestartTitle"), MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("DataDirectory_ErrorTitle"));
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateDriveInformation();
        }

        private MessageBoxResult CreateDataDirectory()
        {
            var selectedDrive = this.DriveSelectionListBox.SelectedItem as DriveInfoDisplay;
            if (selectedDrive is null)
            {
                return MessageBoxResult.None;
            }

            var invalidCharacters = Path.GetInvalidFileNameChars();
            if (string.IsNullOrWhiteSpace(FolderName) || FolderName.Any(c => invalidCharacters.Contains(c)))
            {
                throw new Exception(_globalizer.GetResourceString("DataDirectory_FolderErrorLabel"));
            }

            var newDataFolder = Path.Combine(selectedDrive.DriveInfo.RootDirectory.FullName, FolderName);

            var confirm = MessageBox.Show(string.Format(_globalizer.GetResourceString("Application_DataDirectory_ConfirmLabel"), Path.Combine(newDataFolder, Config.Default.ProfilesRelativePath), Path.Combine(newDataFolder, CommonConfig.Default.SteamCmdRelativePath)), _globalizer.GetResourceString("Application_DataDirectory_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                if (newDataFolder.EndsWith(@"\"))
                    newDataFolder = newDataFolder.Substring(0, newDataFolder.Length - 1);

                Config.Default.DataPath = newDataFolder;
            }

            return confirm;
        }
    }

    public class DriveInfoDisplay
    {
        private const decimal DIVISOR = 1024M;

        // Load all suffixes in an array  
        private static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public DriveInfoDisplay(DriveInfo driveInfo)
        {
            DriveInfo = driveInfo;
        }

        public DriveInfo DriveInfo 
        { 
            get; 
            set; 
        }

        public string Line1
        {
            get
            {
                if (DriveInfo is null)
                    return string.Empty;

                var volumeLabel = string.IsNullOrWhiteSpace(DriveInfo.VolumeLabel) ? _globalizer.GetResourceString("DataDirectory_LocalDiskLabel") : DriveInfo.VolumeLabel;
                return $"{volumeLabel} ({DriveInfo.Name.Replace(@"\", string.Empty)})";
            }
        }

        public string Line2
        {
            get
            {
                if (DriveInfo is null)
                    return string.Empty;

                return string.Format(_globalizer.GetResourceString("DataDirectory_DriveLine2Label"), FormatSize(DriveInfo.TotalFreeSpace), FormatSize(DriveInfo.TotalSize));
            }
        }

        public static string FormatSize(long bytes)
        {
            var counter = 0;
            var number = (decimal)bytes;

            while (number / DIVISOR >= 1)
            {
                number /= DIVISOR;
                counter++;
            }

            return string.Format("{0:n2} {1}", number, suffixes[counter]);
        }
    }
}
