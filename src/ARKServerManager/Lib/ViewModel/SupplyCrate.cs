using System;
using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    public class SupplyCrate : DependencyObject
    {
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(SupplyCrate), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ModProperty = DependencyProperty.Register(nameof(Mod), typeof(string), typeof(SupplyCrate), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty KnownSupplyCrateProperty = DependencyProperty.Register(nameof(KnownSupplyCrate), typeof(bool), typeof(SupplyCrate), new PropertyMetadata(false));

        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        public string Mod
        {
            get { return (string)GetValue(ModProperty); }
            set { SetValue(ModProperty, value); }
        }

        public bool KnownSupplyCrate
        {
            get { return (bool)GetValue(KnownSupplyCrateProperty); }
            set { SetValue(KnownSupplyCrateProperty, value); }
        }

        public string DisplayName => GameData.FriendlySupplyCrateNameForClass(ClassName);

        public string DisplayMod => GameData.FriendlyNameForClass($"Mod_{Mod}", true) ?? Mod;

        public SupplyCrate Duplicate()
        {
            var properties = this.GetType().GetProperties();

            var result = new SupplyCrate();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                    prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }
    }
}
