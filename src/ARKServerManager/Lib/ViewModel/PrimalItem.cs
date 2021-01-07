using ServerManagerTool.Enums;
using System;
using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    public class PrimalItem : DependencyObject
    {
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(PrimalItem), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty ModProperty = DependencyProperty.Register(nameof(Mod), typeof(string), typeof(PrimalItem), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty KnownItemProperty = DependencyProperty.Register(nameof(KnownItem), typeof(bool), typeof(PrimalItem), new PropertyMetadata(false));
        public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(nameof(Category), typeof(string), typeof(PrimalItem), new PropertyMetadata(String.Empty));

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

        public bool KnownItem
        {
            get { return (bool)GetValue(KnownItemProperty); }
            set { SetValue(KnownItemProperty, value); }
        }

        public string Category
        {
            get { return (string)GetValue(CategoryProperty); }
            set { SetValue(CategoryProperty, value); }
        }

        public string DisplayName => GameData.FriendlyItemNameForClass(ClassName);

        public string DisplayMod => GameData.FriendlyNameForClass($"Mod_{Mod}", true) ?? Mod;

        public PrimalItem Duplicate()
        {
            var properties = this.GetType().GetProperties();

            var result = new PrimalItem();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                    prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }
    }
}
