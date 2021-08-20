using NLog;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Lib;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WPFSharp.Globalizer;

namespace ServerManagerTool
{
    /// <summary>
    /// Interaction logic for SupplyCrateOverridesWindow.xaml
    /// </summary>
    public partial class SupplyCrateOverridesWindow : Window
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public EventHandler<ProfileEventArgs> SavePerformed;

        private readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;

        public SupplyCrateOverridesWindow(ServerProfile profile)
        {
            InitializeComponent();
            WindowUtils.RemoveDefaultResourceDictionary(this, Config.Default.DefaultGlobalizationFile);

            this.ServerProfile = profile;
            this.Title = string.Format(_globalizer.GetResourceString("SupplyCrateOverrides_ProfileTitle"), profile?.ProfileName);

            this.DataContext = this;
        }

        public static readonly DependencyProperty ServerProfileProperty = DependencyProperty.Register(nameof(ServerProfile), typeof(ServerProfile), typeof(SupplyCrateOverridesWindow));
        public static readonly DependencyProperty ConfigOverrideSupplyCrateItemsProperty = DependencyProperty.Register(nameof(ConfigOverrideSupplyCrateItems), typeof(SupplyCrateOverrideList), typeof(SupplyCrateOverridesWindow), new PropertyMetadata(null));

        public ServerProfile ServerProfile
        {
            get { return GetValue(ServerProfileProperty) as ServerProfile; }
            set { SetValue(ServerProfileProperty, value); }
        }

        public SupplyCrateOverrideList ConfigOverrideSupplyCrateItems
        {
            get { return (SupplyCrateOverrideList)GetValue(ConfigOverrideSupplyCrateItemsProperty); }
            set { SetValue(ConfigOverrideSupplyCrateItemsProperty, value); }
        }

        #region Event Methods
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearConfigOverrideSupplyCrateItems();
                CloneConfigOverrideSupplyCrateItems(this.ServerProfile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _globalizer.GetResourceString("SupplyCrateOverrides_Load_FailedTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

        private void SupplyCratesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
        }

        private void Collapse_Click(object sender, RoutedEventArgs e)
        {
            SupplyCratesTreeView.UpdateLayout();
            foreach (var item in SupplyCratesTreeView.Items)
            {
                var treeItem = SupplyCratesTreeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeItem != null)
                {
                    treeItem.IsExpanded = false;
                    SetIsExpanded(treeItem, false);
                }
            }
        }

        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            SupplyCratesTreeView.UpdateLayout();
            foreach (var item in SupplyCratesTreeView.Items)
            {
                var treeItem = SupplyCratesTreeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeItem != null)
                {
                    treeItem.ExpandSubtree();
                    //treeItem.IsExpanded = true;
                    //SetIsExpanded(treeItem, true);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveOverrides_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RefreshOverrides_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PasteOverrides_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveOverrides_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ValidateOverrides_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddSupplyCrate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearSupplyCrate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveSupplyCrate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditSupplyCrate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddItemSet_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearItemSet_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditItemSet_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveItemSet_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddItemSetEntry_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearItemSetEntry_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditItemSetEntry_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveItemSetEntry_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddItemEntrySetting_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditItemEntrySetting_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveItemEntrySetting_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region Methods
        protected void OnSavePerformed()
        {
            SavePerformed?.Invoke(this, new ProfileEventArgs(this.ServerProfile));
        }

        public void ClearConfigOverrideSupplyCrateItems()
        {
            this.ConfigOverrideSupplyCrateItems = new SupplyCrateOverrideList(nameof(ConfigOverrideSupplyCrateItems));
            this.ConfigOverrideSupplyCrateItems.Reset();
        }

        private void CloneConfigOverrideSupplyCrateItems(ServerProfile sourceProfile)
        {
            if (sourceProfile == null)
                return;

            sourceProfile.ConfigOverrideSupplyCrateItems.RenderToModel();

            this.ConfigOverrideSupplyCrateItems.Clear();
            this.ConfigOverrideSupplyCrateItems.FromIniValues(sourceProfile.ConfigOverrideSupplyCrateItems.ToIniValues());
            this.ConfigOverrideSupplyCrateItems.IsEnabled = this.ConfigOverrideSupplyCrateItems.Count > 0;
            this.ConfigOverrideSupplyCrateItems.RenderToView();
        }

        private bool CompareConfigOverrideSupplyCrateItems(ServerProfile sourceProfile)
        {
            if (sourceProfile == null)
                return false;

            sourceProfile.ConfigOverrideSupplyCrateItems.RenderToModel();
            var sourceIniValue = sourceProfile.ConfigOverrideSupplyCrateItems.ToIniValues();

            this.ConfigOverrideSupplyCrateItems.RenderToModel();
            var iniValue = this.ConfigOverrideSupplyCrateItems.ToIniValues();

            return Equals(sourceIniValue, iniValue);
        }
        
        private void SetIsExpanded(TreeViewItem treeViewItem, bool isExpanded)
        {
            foreach (var item in treeViewItem.Items)
            {
                var childControl = treeViewItem.ItemContainerGenerator.ContainerFromItem(item) as ItemsControl;
                if (childControl != null)
                {
                    var treeItem = childControl as TreeViewItem;
                    if (treeItem != null)
                        treeItem.IsExpanded = isExpanded;
                    SetIsExpanded(treeItem, isExpanded);
                }
            }
        }
        #endregion
    }
}
