using ServerManagerTool.Common.Model;
using ServerManagerTool.Enums;
using System;
using System.Linq;

namespace ServerManagerTool.Lib.ViewModel
{
    public class DinoSettingsList : SortableObservableCollection<DinoSettings>
    {
        public AggregateIniValueList<DinoSpawn> DinoSpawnWeightMultipliers { get; }
        public StringIniValueList PreventDinoTameClassNames { get; }
        public StringIniValueList PreventBreedingForClassNames { get; }
        public AggregateIniValueList<NPCReplacement> NpcReplacements { get; }
        public AggregateIniValueList<ClassMultiplier> TamedDinoClassDamageMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> TamedDinoClassResistanceMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> DinoClassDamageMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> DinoClassResistanceMultipliers { get; }

        public DinoSettingsList()
        {
            Reset();
        }

        public DinoSettingsList(AggregateIniValueList<DinoSpawn> dinoSpawnWeightMultipliers, StringIniValueList preventDinoTameClassNames, StringIniValueList preventBreedingForClassNames, AggregateIniValueList<NPCReplacement> npcReplacements,
                                AggregateIniValueList<ClassMultiplier> tamedDinoClassDamageMultipliers, AggregateIniValueList<ClassMultiplier> tamedDinoClassResistanceMultipliers,
                                AggregateIniValueList<ClassMultiplier> dinoClassDamageMultipliers, AggregateIniValueList<ClassMultiplier> dinoClassResistanceMultipliers)
        {
            this.DinoSpawnWeightMultipliers = dinoSpawnWeightMultipliers;
            this.PreventDinoTameClassNames = preventDinoTameClassNames;
            this.PreventBreedingForClassNames = preventBreedingForClassNames;
            this.NpcReplacements = npcReplacements;
            this.TamedDinoClassDamageMultipliers = tamedDinoClassDamageMultipliers;
            this.TamedDinoClassResistanceMultipliers = tamedDinoClassResistanceMultipliers;
            this.DinoClassDamageMultipliers = dinoClassDamageMultipliers;
            this.DinoClassResistanceMultipliers = dinoClassResistanceMultipliers;
            Reset();
        }

        private DinoSettings CreateDinoSetting(string className, string mod, bool knownDino, bool hasNameTag, bool hasClassName)
        {
            var nameTag = GameData.NameTagForClass(className);
            var isSpawnable = GameData.IsSpawnableForClass(className);
            var isTameable = GameData.IsTameableForClass(className);
            var isBreedingable = GameData.IsBreedingableForClass(className);

            return new DinoSettings()
            {
                ClassName = className,
                Mod = mod,
                KnownDino = knownDino,
                NameTag = nameTag,

                CanSpawn = true,
                CanTame = isTameable != DinoTamable.False,
                CanBreeding = isBreedingable != DinoBreedingable.False,
                ReplacementClass = className,

                SpawnWeightMultiplier = DinoSpawn.DEFAULT_SPAWN_WEIGHT_MULTIPLIER,
                OverrideSpawnLimitPercentage = DinoSpawn.DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE,
                SpawnLimitPercentage = DinoSpawn.DEFAULT_SPAWN_LIMIT_PERCENTAGE,
                OriginalSpawnWeightMultiplier = DinoSpawn.DEFAULT_SPAWN_WEIGHT_MULTIPLIER,
                OriginalOverrideSpawnLimitPercentage = DinoSpawn.DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE,
                OriginalSpawnLimitPercentage = DinoSpawn.DEFAULT_SPAWN_LIMIT_PERCENTAGE,

                TamedDamageMultiplier = ClassMultiplier.DEFAULT_MULTIPLIER,
                TamedResistanceMultiplier = ClassMultiplier.DEFAULT_MULTIPLIER,
                WildDamageMultiplier = ClassMultiplier.DEFAULT_MULTIPLIER,
                WildResistanceMultiplier = ClassMultiplier.DEFAULT_MULTIPLIER,

                HasClassName = hasClassName,
                HasNameTag = hasNameTag,
                IsSpawnable = isSpawnable,
                IsTameable = isTameable,
                IsBreedingable = isBreedingable,
            };
        }

        public void Reset()
        {
            this.Clear();

            var dinoSpawns = GameData.GetDinoSpawns();
            foreach (var entry in dinoSpawns)
            {
                this.Add(CreateDinoSetting(entry.ClassName, entry.Mod, entry.KnownDino, entry.DinoNameTag != null, true));
            }

            Sort(d => d.NameSort);
        }

        public void RenderToView()
        {
            Reset();

            foreach(var entry in this.DinoSpawnWeightMultipliers.Where(e => !string.IsNullOrWhiteSpace(e.DinoNameTag)))
            {
                if (this.Any(d => d.NameTag == entry.DinoNameTag))
                {
                    foreach (var dinoSetting in this.Where(d => d.NameTag == entry.DinoNameTag))
                    {
                        dinoSetting.SpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                        dinoSetting.OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                        dinoSetting.SpawnLimitPercentage = entry.SpawnLimitPercentage;

                        dinoSetting.OriginalSpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                        dinoSetting.OriginalOverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                        dinoSetting.OriginalSpawnLimitPercentage = entry.SpawnLimitPercentage;
                    }
                }
                else
                {
                    var dinoSetting = CreateDinoSetting(entry.DinoNameTag, entry.Mod, entry.KnownDino, true, false);
                    dinoSetting.SpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                    dinoSetting.OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                    dinoSetting.SpawnLimitPercentage = entry.SpawnLimitPercentage;

                    dinoSetting.OriginalSpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                    dinoSetting.OriginalOverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                    dinoSetting.OriginalSpawnLimitPercentage = entry.SpawnLimitPercentage;

                    this.Add(dinoSetting);
                }
            }

            foreach(var entry in this.PreventDinoTameClassNames.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                if (this.Any(d => d.ClassName == entry))
                {
                    foreach (var dinoSetting in this.Where(d => d.ClassName == entry && d.CanTame))
                    {
                        dinoSetting.CanTame = false;
                    }
                }
                else
                {
                    var dinoSetting = CreateDinoSetting(entry, GameData.MOD_UNKNOWN, false, false, true);
                    dinoSetting.CanTame = false;

                    this.Add(dinoSetting);
                }
            }

            foreach (var entry in this.PreventBreedingForClassNames.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                if (this.Any(d => d.ClassName == entry))
                {
                    foreach (var dinoSetting in this.Where(d => d.ClassName == entry && d.CanBreeding))
                    {
                        dinoSetting.CanBreeding = false;
                    }
                }
                else
                {
                    var dinoSetting = CreateDinoSetting(entry, GameData.MOD_UNKNOWN, false, false, true);
                    dinoSetting.CanBreeding = false;

                    this.Add(dinoSetting);
                }
            }

            foreach (var entry in this.NpcReplacements.Where(e => !string.IsNullOrWhiteSpace(e.FromClassName)))
            {
                if (this.Any(d => d.ClassName == entry.FromClassName))
                {
                    foreach (var dinoSetting in this.Where(d => d.ClassName == entry.FromClassName))
                    {
                        dinoSetting.CanSpawn = !string.IsNullOrWhiteSpace(entry.ToClassName);
                        dinoSetting.ReplacementClass = dinoSetting.CanSpawn ? entry.ToClassName : dinoSetting.ClassName;
                    }
                }
                else
                {
                    var dinoSetting = CreateDinoSetting(entry.FromClassName, GameData.MOD_UNKNOWN, false, false, true);
                    dinoSetting.CanSpawn = !string.IsNullOrWhiteSpace(entry.ToClassName);
                    dinoSetting.ReplacementClass = dinoSetting.CanSpawn ? entry.ToClassName : dinoSetting.ClassName;

                    this.Add(dinoSetting);
                }
            }

            foreach (var entry in this.TamedDinoClassDamageMultipliers.Where(e => !string.IsNullOrWhiteSpace(e.ClassName)))
            {
                if (this.Any(d => d.ClassName == entry.ClassName))
                {
                    foreach (var dinoSetting in this.Where(d => d.ClassName == entry.ClassName && d.TamedDamageMultiplier != entry.Multiplier))
                    {
                        dinoSetting.TamedDamageMultiplier = entry.Multiplier;
                    }
                }
                else
                {
                    var dinoSetting = CreateDinoSetting(entry.ClassName, GameData.MOD_UNKNOWN, false, false, true);
                    dinoSetting.TamedDamageMultiplier = entry.Multiplier;

                    this.Add(dinoSetting);
                }
            }

            foreach(var entry in this.TamedDinoClassResistanceMultipliers.Where(e => !string.IsNullOrWhiteSpace(e.ClassName)))
            {
                if (this.Any(d => d.ClassName == entry.ClassName))
                {
                    foreach (var dinoSetting in this.Where(d => d.ClassName == entry.ClassName && d.TamedResistanceMultiplier != entry.Multiplier))
                    {
                        dinoSetting.TamedResistanceMultiplier = entry.Multiplier;
                    }
                }
                else
                {
                    var dinoSetting = CreateDinoSetting(entry.ClassName, GameData.MOD_UNKNOWN, false, false, true);
                    dinoSetting.TamedResistanceMultiplier = entry.Multiplier;

                    this.Add(dinoSetting);
                }
            }

            foreach (var entry in this.DinoClassDamageMultipliers.Where(e => !string.IsNullOrWhiteSpace(e.ClassName)))
            {
                if (this.Any(d => d.ClassName == entry.ClassName))
                {
                    foreach (var dinoSetting in this.Where(d => d.ClassName == entry.ClassName && d.WildDamageMultiplier != entry.Multiplier))
                    {
                        dinoSetting.WildDamageMultiplier = entry.Multiplier;
                    }
                }
                else
                {
                    var dinoSetting = CreateDinoSetting(entry.ClassName, GameData.MOD_UNKNOWN, false, false, true);
                    dinoSetting.WildDamageMultiplier = entry.Multiplier;

                    this.Add(dinoSetting);
                }
            }

            foreach (var entry in this.DinoClassResistanceMultipliers.Where(e => !string.IsNullOrWhiteSpace(e.ClassName)))
            {
                if (this.Any(d => d.ClassName == entry.ClassName))
                {
                    foreach (var dinoSetting in this.Where(d => d.ClassName == entry.ClassName && d.WildResistanceMultiplier != entry.Multiplier))
                    {
                        dinoSetting.WildResistanceMultiplier = entry.Multiplier;
                    }
                }
                else
                {
                    var dinoSetting = CreateDinoSetting(entry.ClassName, GameData.MOD_UNKNOWN, false, false, true);
                    dinoSetting.WildResistanceMultiplier = entry.Multiplier;

                    this.Add(dinoSetting);
                }
            }

            Sort(d => d.NameSort);
        }

        public void RenderToModel()
        {
            this.DinoSpawnWeightMultipliers.Clear();
            this.PreventDinoTameClassNames.Clear();
            this.PreventDinoTameClassNames.IsEnabled = true;
            this.PreventBreedingForClassNames.Clear();
            this.PreventBreedingForClassNames.IsEnabled = true;
            this.NpcReplacements.Clear();
            this.NpcReplacements.IsEnabled = true;
            this.TamedDinoClassDamageMultipliers.Clear();
            this.TamedDinoClassResistanceMultipliers.Clear();
            this.DinoClassDamageMultipliers.Clear();
            this.DinoClassResistanceMultipliers.Clear();
                       
            foreach(var entry in this)
            {
                if (entry.HasNameTag && !string.IsNullOrWhiteSpace(entry.NameTag))
                {
                    if (!entry.KnownDino ||
                        !entry.OverrideSpawnLimitPercentage.Equals(DinoSpawn.DEFAULT_OVERRIDE_SPAWN_LIMIT_PERCENTAGE) ||
                        !entry.SpawnLimitPercentage.Equals(DinoSpawn.DEFAULT_SPAWN_LIMIT_PERCENTAGE) ||
                        !entry.SpawnWeightMultiplier.Equals(DinoSpawn.DEFAULT_SPAWN_WEIGHT_MULTIPLIER))
                    {
                        if (this.DinoSpawnWeightMultipliers.Any(d => d.DinoNameTag.Equals(entry.NameTag, StringComparison.OrdinalIgnoreCase)))
                        {
                            foreach (var dinoSpawn in this.DinoSpawnWeightMultipliers.Where(d => d.DinoNameTag.Equals(entry.NameTag, StringComparison.OrdinalIgnoreCase)))
                            {
                                if (entry.SpawnWeightMultiplier != entry.OriginalSpawnWeightMultiplier || 
                                    entry.OverrideSpawnLimitPercentage != entry.OriginalOverrideSpawnLimitPercentage ||
                                    entry.SpawnLimitPercentage != entry.OriginalSpawnLimitPercentage)
                                {
                                    dinoSpawn.OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                                    dinoSpawn.SpawnLimitPercentage = entry.SpawnLimitPercentage;
                                    dinoSpawn.SpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                                }
                            }
                        }
                        else
                        {
                            this.DinoSpawnWeightMultipliers.Add(new DinoSpawn()
                            {
                                ClassName = entry.ClassName,
                                DinoNameTag = entry.NameTag,
                                OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage,
                                SpawnLimitPercentage = entry.SpawnLimitPercentage,
                                SpawnWeightMultiplier = entry.SpawnWeightMultiplier
                            });
                        }
                    }
                }

                if (entry.HasClassName && !string.IsNullOrWhiteSpace(entry.ClassName))
                {
                    if ((entry.IsTameable != DinoTamable.False) && !entry.CanTame)
                    {
                        this.PreventDinoTameClassNames.Add(entry.ClassName);
                    }

                    if ((entry.IsBreedingable != DinoBreedingable.False) && !entry.CanBreeding)
                    {
                        this.PreventBreedingForClassNames.Add(entry.ClassName);
                    }

                    this.NpcReplacements.Add(new NPCReplacement() { FromClassName = entry.ClassName, ToClassName = entry.CanSpawn ? entry.ReplacementClass : string.Empty });

                    if (entry.IsTameable != DinoTamable.False)
                    {
                        // check if the value has changed.
                        if (!entry.TamedDamageMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                        {
                            this.TamedDinoClassDamageMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.TamedDamageMultiplier });
                        }

                        // check if the value has changed.
                        if (!entry.TamedResistanceMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                        {
                            this.TamedDinoClassResistanceMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.TamedResistanceMultiplier });
                        }
                    }

                    // check if the value has changed.
                    if (!entry.WildDamageMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                    {
                        this.DinoClassDamageMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.WildDamageMultiplier });
                    }

                    // check if the value has changed.
                    if (!entry.WildResistanceMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                    {
                        this.DinoClassResistanceMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.WildResistanceMultiplier });
                    }
                }
            }
        }
    }
}
