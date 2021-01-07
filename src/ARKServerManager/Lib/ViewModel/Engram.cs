using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    public class Engram : DependencyObject
    {
        public static readonly DependencyProperty EngramClassNameProperty = DependencyProperty.Register(nameof(EngramClassName), typeof(string), typeof(Engram), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty EngramLevelRequirementProperty = DependencyProperty.Register(nameof(EngramLevelRequirement), typeof(int), typeof(Engram), new PropertyMetadata(0));
        public static readonly DependencyProperty EngramPointsCostProperty = DependencyProperty.Register(nameof(EngramPointsCost), typeof(int), typeof(Engram), new PropertyMetadata(0));
        public static readonly DependencyProperty ModProperty = DependencyProperty.Register(nameof(Mod), typeof(string), typeof(Engram), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty KnownEngramProperty = DependencyProperty.Register(nameof(KnownEngram), typeof(bool), typeof(Engram), new PropertyMetadata(false));
        public static readonly DependencyProperty IsTekgramProperty = DependencyProperty.Register(nameof(IsTekgram), typeof(bool), typeof(EngramEntry), new PropertyMetadata(false));

        public string EngramClassName
        {
            get { return (string)GetValue(EngramClassNameProperty); }
            set { SetValue(EngramClassNameProperty, value); }
        }

        public int EngramLevelRequirement
        {
            get { return (int)GetValue(EngramLevelRequirementProperty); }
            set { SetValue(EngramLevelRequirementProperty, value); }
        }

        public int EngramPointsCost
        {
            get { return (int)GetValue(EngramPointsCostProperty); }
            set { SetValue(EngramPointsCostProperty, value); }
        }

        public string Mod
        {
            get { return (string)GetValue(ModProperty); }
            set { SetValue(ModProperty, value); }
        }

        public bool KnownEngram
        {
            get { return (bool)GetValue(KnownEngramProperty); }
            set { SetValue(KnownEngramProperty, value); }
        }

        public bool IsTekgram
        {
            get { return (bool)GetValue(IsTekgramProperty); }
            set { SetValue(IsTekgramProperty, value); }
        }

        public string DisplayName => GameData.FriendlyEngramNameForClass(EngramClassName);

        public string DisplayMod => GameData.FriendlyNameForClass($"Mod_{Mod}", true) ?? Mod;

        public Engram Duplicate()
        {
            var properties = this.GetType().GetProperties();

            var result = new Engram();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                    prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }
    }
}
