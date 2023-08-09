using ServerManagerTool.Enums;
using System;
using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    //
    // This class aggregates many settings related to dinos
    //
    public class DinoSettings : DependencyObject
    {
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty ModProperty = DependencyProperty.Register(nameof(Mod), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty KnownDinoProperty = DependencyProperty.Register(nameof(KnownDino), typeof(bool), typeof(DinoSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty CanTameProperty = DependencyProperty.Register(nameof(CanTame), typeof(bool), typeof(DinoSettings), new PropertyMetadata(true));
        public static readonly DependencyProperty CanBreedingProperty = DependencyProperty.Register(nameof(CanBreeding), typeof(bool), typeof(DinoSettings), new PropertyMetadata(true));
        public static readonly DependencyProperty CanSpawnProperty = DependencyProperty.Register(nameof(CanSpawn), typeof(bool), typeof(DinoSettings), new PropertyMetadata(true));
        public static readonly DependencyProperty ReplacementClassProperty = DependencyProperty.Register(nameof(ReplacementClass), typeof(string), typeof(DinoSettings), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty SpawnWeightMultiplierProperty = DependencyProperty.Register(nameof(SpawnWeightMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(DinoSpawn.DEFAULT_SPAWN_WEIGHT_MULTIPLIER));
        public static readonly DependencyProperty OverrideSpawnLimitPercentageProperty = DependencyProperty.Register(nameof(OverrideSpawnLimitPercentage), typeof(bool), typeof(DinoSettings), new PropertyMetadata(DinoSpawn.DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE));
        public static readonly DependencyProperty SpawnLimitPercentageProperty = DependencyProperty.Register(nameof(SpawnLimitPercentage), typeof(float), typeof(DinoSettings), new PropertyMetadata(DinoSpawn.DEFAULT_SPAWN_LIMIT_PERCENTAGE));
        public static readonly DependencyProperty TamedDamageMultiplierProperty = DependencyProperty.Register(nameof(TamedDamageMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(ClassMultiplier.DEFAULT_MULTIPLIER));
        public static readonly DependencyProperty TamedResistanceMultiplierProperty = DependencyProperty.Register(nameof(TamedResistanceMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(ClassMultiplier.DEFAULT_MULTIPLIER));
        public static readonly DependencyProperty WildDamageMultiplierProperty = DependencyProperty.Register(nameof(WildDamageMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(ClassMultiplier.DEFAULT_MULTIPLIER));
        public static readonly DependencyProperty WildResistanceMultiplierProperty = DependencyProperty.Register(nameof(WildResistanceMultiplier), typeof(float), typeof(DinoSettings), new PropertyMetadata(ClassMultiplier.DEFAULT_MULTIPLIER));

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

        public bool KnownDino
        {
            get { return (bool)GetValue(KnownDinoProperty); }
            set { SetValue(KnownDinoProperty, value); }
        }

        public bool CanTame
        {
            get { return (bool)GetValue(CanTameProperty); }
            set { SetValue(CanTameProperty, value); }
        }

        public bool CanBreeding
        {
            get { return (bool)GetValue(CanBreedingProperty); }
            set { SetValue(CanBreedingProperty, value); }
        }

        public bool CanSpawn
        {
            get { return (bool)GetValue(CanSpawnProperty); }
            set { SetValue(CanSpawnProperty, value); }
        }

        public string ReplacementClass
        {
            get { return (string)GetValue(ReplacementClassProperty); }
            set { SetValue(ReplacementClassProperty, value); }
        }

        public float SpawnWeightMultiplier
        {
            get { return (float)GetValue(SpawnWeightMultiplierProperty); }
            set { SetValue(SpawnWeightMultiplierProperty, value); }
        }

        public bool OverrideSpawnLimitPercentage
        {
            get { return (bool)GetValue(OverrideSpawnLimitPercentageProperty); }
            set { SetValue(OverrideSpawnLimitPercentageProperty, value); }
        }

        public float SpawnLimitPercentage
        {
            get { return (float)GetValue(SpawnLimitPercentageProperty); }
            set { SetValue(SpawnLimitPercentageProperty, value); }
        }

        public float TamedDamageMultiplier
        {
            get { return (float)GetValue(TamedDamageMultiplierProperty); }
            set { SetValue(TamedDamageMultiplierProperty, value); }
        }

        public float TamedResistanceMultiplier
        {
            get { return (float)GetValue(TamedResistanceMultiplierProperty); }
            set { SetValue(TamedResistanceMultiplierProperty, value); }
        }

        public float WildDamageMultiplier
        {
            get { return (float)GetValue(WildDamageMultiplierProperty); }
            set { SetValue(WildDamageMultiplierProperty, value); }
        }
       
        public float WildResistanceMultiplier
        {
            get { return (float)GetValue(WildResistanceMultiplierProperty); }
            set { SetValue(WildResistanceMultiplierProperty, value); }
        }

        public string DisplayName => GameData.FriendlyCreatureNameForClass(ClassName);
        public string DisplayMod => GameData.FriendlyNameForClass($"Mod_{Mod}", true) ?? Mod;
        public string NameTag { get; internal set; }
        public bool HasNameTag { get; internal set; }
        public bool HasClassName { get; internal set; }
        public bool IsSpawnable { get; internal set; }
        public DinoTamable IsTameable { get; internal set; }
        public DinoBreedingable IsBreedingable { get; internal set; }
        public string DisplayReplacementName => GameData.FriendlyCreatureNameForClass(ReplacementClass);

        public float OriginalSpawnWeightMultiplier { get; internal set; }
        public bool OriginalOverrideSpawnLimitPercentage { get; internal set; }
        public float OriginalSpawnLimitPercentage { get; internal set; }

        #region Sort Properties
        public string NameSort => $"{DisplayName}|{Mod}";
        public string ModSort => $"{Mod}|{DisplayName}";
        public string CanSpawnSort => $"{IsSpawnable}|{CanSpawn}|{DisplayName}|{Mod}";
        public string CanTameSort => $"{IsTameable != DinoTamable.False}|{CanTame}|{DisplayName}|{Mod}";
        public string CanBreedingSort => $"{IsBreedingable != DinoBreedingable.False}|{CanBreeding}|{DisplayName}|{Mod}";
        public string ReplacementNameSort => $"{DisplayReplacementName}|{Mod}";
        public string SpawnWeightMultiplierSort => $"{SpawnWeightMultiplier:0000000000.0000000000}|{DisplayName}|{Mod}";
        public string OverrideSpawnLimitPercentageSort => $"{OverrideSpawnLimitPercentage}|{DisplayName}|{Mod}";
        public string SpawnLimitPercentageSort => $"{SpawnLimitPercentage:0000000000.0000000000}|{DisplayName}|{Mod}";
        public string TamedDamageMultiplierSort => $"{TamedDamageMultiplier:0000000000.0000000000}|{DisplayName}|{Mod}";
        public string TamedResistanceMultiplierSort => $"{TamedResistanceMultiplier:0000000000.0000000000}|{DisplayName}|{Mod}";
        public string WildDamageMultiplierSort => $"{WildDamageMultiplier:0000000000.0000000000}|{DisplayName}|{Mod}";
        public string WildResistanceMultiplierSort => $"{WildResistanceMultiplier:0000000000.0000000000}|{DisplayName}|{Mod}";
        #endregion
    }
}
