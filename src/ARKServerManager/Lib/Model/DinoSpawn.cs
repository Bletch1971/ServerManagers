using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Enums;
using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml.Serialization;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class DinoSpawn : AggregateIniValue
    {
        public const bool DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE = true;
        public const float DEFAULT_SPAWN_LIMIT_PERCENTAGE = ClassMultiplier.DEFAULT_MULTIPLIER;
        public const float DEFAULT_SPAWN_WEIGHT_MULTIPLIER = ClassMultiplier.DEFAULT_MULTIPLIER;

        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(DinoSpawn), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty ModProperty = DependencyProperty.Register(nameof(Mod), typeof(string), typeof(DinoSpawn), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty KnownDinoProperty = DependencyProperty.Register(nameof(KnownDino), typeof(bool), typeof(DinoSpawn), new PropertyMetadata(false));
        public static readonly DependencyProperty DinoNameTagProperty = DependencyProperty.Register(nameof(DinoNameTag), typeof(string), typeof(DinoSpawn), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty OverrideSpawnLimitPercentageProperty = DependencyProperty.Register(nameof(OverrideSpawnLimitPercentage), typeof(bool), typeof(DinoSpawn), new PropertyMetadata(DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE));
        public static readonly DependencyProperty SpawnLimitPercentageProperty = DependencyProperty.Register(nameof(SpawnLimitPercentage), typeof(float), typeof(DinoSpawn), new PropertyMetadata(DEFAULT_SPAWN_LIMIT_PERCENTAGE));
        public static readonly DependencyProperty SpawnWeightMultiplierProperty = DependencyProperty.Register(nameof(SpawnWeightMultiplier), typeof(float), typeof(DinoSpawn), new PropertyMetadata(DEFAULT_SPAWN_WEIGHT_MULTIPLIER));

        [DataMember]
        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        [DataMember]
        public string Mod
        {
            get { return (string)GetValue(ModProperty); }
            set { SetValue(ModProperty, value); }
        }

        [DataMember]
        public bool KnownDino
        {
            get { return (bool)GetValue(KnownDinoProperty); }
            set { SetValue(KnownDinoProperty, value); }
        }

        [XmlElement(ElementName="Name")]
        [DataMember]
        [AggregateIniValueEntry]
        public string DinoNameTag
        {
            get { return (string)GetValue(DinoNameTagProperty); }
            set { SetValue(DinoNameTagProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public bool OverrideSpawnLimitPercentage
        {
            get { return (bool)GetValue(OverrideSpawnLimitPercentageProperty); }
            set { SetValue(OverrideSpawnLimitPercentageProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public float SpawnLimitPercentage
        {
            get { return (float)GetValue(SpawnLimitPercentageProperty); }
            set { SetValue(SpawnLimitPercentageProperty, value); }  
        }

        [DataMember]
        [AggregateIniValueEntry]
        public float SpawnWeightMultiplier
        {
            get { return (float)GetValue(SpawnWeightMultiplierProperty); }
            set { SetValue(SpawnWeightMultiplierProperty, value); }
        }

        public string DisplayName => GameData.FriendlyCreatureNameForClass(ClassName);

        public string DisplayMod => GameData.FriendlyNameForClass($"Mod_{Mod}", true) ?? Mod;

        public static DinoSpawn FromINIValue(string iniValue)
        {
            var newSpawn = new DinoSpawn();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override string GetSortKey()
        {
            return this.DinoNameTag;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return String.Equals(this.DinoNameTag, ((DinoSpawn)other).DinoNameTag, StringComparison.OrdinalIgnoreCase);
        }
    }
}
