using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class ClassMultiplier : AggregateIniValue
    {
        public const float DEFAULT_MULTIPLIER = 1.0f;

        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(ClassMultiplier), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty MultiplierProperty = DependencyProperty.Register(nameof(Multiplier), typeof(float), typeof(ClassMultiplier), new PropertyMetadata(DEFAULT_MULTIPLIER));

        [DataMember]
        [AggregateIniValueEntry]
        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public float Multiplier
        {
            get { return (float)GetValue(MultiplierProperty); }
            set { SetValue(MultiplierProperty, value); }
        }

        public virtual string DisplayName => GameData.FriendlyNameForClass(ClassName);

        public static ClassMultiplier FromINIValue(string iniValue)
        {
            var newSpawn = new ClassMultiplier();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override string GetSortKey() => GameData.FriendlyNameForClass(this.ClassName);

        public override bool IsEquivalent(AggregateIniValue other) => String.Equals(this.ClassName, ((ClassMultiplier)other).ClassName, StringComparison.OrdinalIgnoreCase);
    }
}
