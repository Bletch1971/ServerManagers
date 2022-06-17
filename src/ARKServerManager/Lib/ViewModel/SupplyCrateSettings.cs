using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    public class SupplyCrateItemEntrySettings : DependencyObject
    {
        public static readonly DependencyProperty ItemClassStringProperty = DependencyProperty.Register(nameof(ItemClassString), typeof(string), typeof(SupplyCrateItemEntrySettings), new PropertyMetadata(string.Empty));
        public string ItemClassString
        {
            get { return (string)GetValue(ItemClassStringProperty); }
            set { SetValue(ItemClassStringProperty, value); }
        }

        public static readonly DependencyProperty ItemWeightProperty = DependencyProperty.Register(nameof(ItemWeight), typeof(float), typeof(SupplyCrateItemEntrySettings), new PropertyMetadata(1.0f));
        public float ItemWeight
        {
            get { return (float)GetValue(ItemWeightProperty); }
            set { SetValue(ItemWeightProperty, value); }
        }

        public string DisplayName => GameData.FriendlyItemNameForClass(ItemClassString);

        public string DisplayNameFull
        {
            get
            {
                var modName = GameData.FriendlyItemModNameForClass(ItemClassString); ;
                return $"{(string.IsNullOrWhiteSpace(modName) ? string.Empty : $"({modName}) ")}{DisplayName}";
            }
        }

        public bool IsViewValid => !string.IsNullOrWhiteSpace(ItemClassString);

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(SupplyCrateItemEntrySettings), new PropertyMetadata("N"));
        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update()
        {
            ValidStatus = IsViewValid 
                ? (GameData.HasItemForClass(ItemClassString) 
                    ? "Y" 
                    : "W") 
                : "N";
        }
    }
}
