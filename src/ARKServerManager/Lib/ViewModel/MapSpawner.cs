using System;
using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    public class MapSpawner : DependencyObject
    {
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(MapSpawner), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ModProperty = DependencyProperty.Register(nameof(Mod), typeof(string), typeof(MapSpawner), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty KnownSpawnerProperty = DependencyProperty.Register(nameof(KnownSpawner), typeof(bool), typeof(MapSpawner), new PropertyMetadata(false));

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

        public bool KnownSpawner
        {
            get { return (bool)GetValue(KnownSpawnerProperty); }
            set { SetValue(KnownSpawnerProperty, value); }
        }

        public string DisplayName => GameData.FriendlyMapSpawnerNameForClass(ClassName);

        public string DisplayMod => GameData.FriendlyNameForClass($"Mod_{Mod}", true) ?? Mod;

        public MapSpawner Duplicate()
        {
            var properties = this.GetType().GetProperties();

            var result = new MapSpawner();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                    prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }
    }
}
