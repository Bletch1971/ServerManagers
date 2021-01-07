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
        public AggregateIniValueList<NPCReplacement> NpcReplacements { get; }
        public AggregateIniValueList<ClassMultiplier> TamedDinoClassDamageMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> TamedDinoClassResistanceMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> DinoClassDamageMultipliers { get; }
        public AggregateIniValueList<ClassMultiplier> DinoClassResistanceMultipliers { get; }

        public DinoSettingsList()
        {
            Reset();
        }

        public DinoSettingsList(AggregateIniValueList<DinoSpawn> dinoSpawnWeightMultipliers, StringIniValueList preventDinoTameClassNames, AggregateIniValueList<NPCReplacement> npcReplacements,
                                AggregateIniValueList<ClassMultiplier> tamedDinoClassDamageMultipliers, AggregateIniValueList<ClassMultiplier> tamedDinoClassResistanceMultipliers,
                                AggregateIniValueList<ClassMultiplier> dinoClassDamageMultipliers, AggregateIniValueList<ClassMultiplier> dinoClassResistanceMultipliers)
        {
            this.DinoSpawnWeightMultipliers = dinoSpawnWeightMultipliers;
            this.PreventDinoTameClassNames = preventDinoTameClassNames;
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

            return new DinoSettings()
            {
                ClassName = className,
                Mod = mod,
                KnownDino = knownDino,
                NameTag = nameTag,

                CanSpawn = true,
                CanTame = isTameable != DinoTamable.False,
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

            foreach(var entry in this.DinoSpawnWeightMultipliers)
            {
                if (string.IsNullOrWhiteSpace(entry.DinoNameTag))
                    continue;

                var dinoSettings = this.Where(vi => vi.NameTag == entry.DinoNameTag).ToArray();
                if (dinoSettings == null || dinoSettings.Length == 0)
                {
                    this.Add(CreateDinoSetting(entry.DinoNameTag, entry.Mod, entry.KnownDino, true, false));
                }

                dinoSettings = this.Where(vi => vi.NameTag == entry.DinoNameTag).ToArray();
                foreach (var dinoSetting in dinoSettings)
                {
                    dinoSetting.SpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                    dinoSetting.OverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                    dinoSetting.SpawnLimitPercentage = entry.SpawnLimitPercentage;

                    dinoSetting.OriginalSpawnWeightMultiplier = entry.SpawnWeightMultiplier;
                    dinoSetting.OriginalOverrideSpawnLimitPercentage = entry.OverrideSpawnLimitPercentage;
                    dinoSetting.OriginalSpawnLimitPercentage = entry.SpawnLimitPercentage;
                }
            }

            foreach(var entry in this.PreventDinoTameClassNames)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                var dinoSettings = this.Where(vi => vi.ClassName == entry).ToArray();
                if (dinoSettings == null || dinoSettings.Length == 0)
                {
                    this.Add(CreateDinoSetting(entry, GameData.MOD_UNKNOWN, false, false, true));
                }

                dinoSettings = this.Where(vi => vi.ClassName == entry).ToArray();
                foreach (var dinoSetting in dinoSettings)
                {
                    dinoSetting.CanTame = false;
                }
            }

            foreach(var entry in this.NpcReplacements)
            {
                if (string.IsNullOrWhiteSpace(entry.FromClassName))
                    continue;

                var dinoSettings = this.Where(vi => vi.ClassName == entry.FromClassName).ToArray();
                if (dinoSettings == null || dinoSettings.Length == 0)
                {
                    this.Add(CreateDinoSetting(entry.FromClassName, GameData.MOD_UNKNOWN, false, false, true));
                }

                dinoSettings = this.Where(vi => vi.ClassName == entry.FromClassName).ToArray();
                foreach (var dinoSetting in dinoSettings)
                {
                    dinoSetting.CanSpawn = !string.IsNullOrWhiteSpace(entry.ToClassName);
                    dinoSetting.ReplacementClass = dinoSetting.CanSpawn ? entry.ToClassName : dinoSetting.ClassName;
                }
            }

            foreach (var entry in this.TamedDinoClassDamageMultipliers)
            {
                if (string.IsNullOrWhiteSpace(entry.ClassName))
                    continue;

                var dinoSettings = this.Where(vi => vi.ClassName == entry.ClassName).ToArray();
                if (dinoSettings == null || dinoSettings.Length == 0)
                {
                    this.Add(CreateDinoSetting(entry.ClassName, GameData.MOD_UNKNOWN, false, false, true));
                }

                dinoSettings = this.Where(vi => vi.ClassName == entry.ClassName).ToArray();
                foreach (var dinoSetting in dinoSettings)
                {
                    dinoSetting.TamedDamageMultiplier = entry.Multiplier;
                }
            }

            foreach(var entry in this.TamedDinoClassResistanceMultipliers)
            {
                if (string.IsNullOrWhiteSpace(entry.ClassName))
                    continue;

                var dinoSettings = this.Where(vi => vi.ClassName == entry.ClassName).ToArray();
                if (dinoSettings == null || dinoSettings.Length == 0)
                {
                    this.Add(CreateDinoSetting(entry.ClassName, GameData.MOD_UNKNOWN, false, false, true));
                }

                dinoSettings = this.Where(vi => vi.ClassName == entry.ClassName).ToArray();
                foreach (var dinoSetting in dinoSettings)
                {
                    dinoSetting.TamedResistanceMultiplier = entry.Multiplier;
                }
            }

            foreach (var entry in this.DinoClassDamageMultipliers)
            {
                if (string.IsNullOrWhiteSpace(entry.ClassName))
                    continue;

                var dinoSettings = this.Where(vi => vi.ClassName == entry.ClassName).ToArray();
                if (dinoSettings == null || dinoSettings.Length == 0)
                {
                    this.Add(CreateDinoSetting(entry.ClassName, GameData.MOD_UNKNOWN, false, false, true));
                }

                dinoSettings = this.Where(vi => vi.ClassName == entry.ClassName).ToArray();
                foreach (var dinoSetting in dinoSettings)
                {
                    dinoSetting.WildDamageMultiplier = entry.Multiplier;
                }
            }

            foreach (var entry in this.DinoClassResistanceMultipliers)
            {
                if (string.IsNullOrWhiteSpace(entry.ClassName))
                    continue;

                var dinoSettings = this.Where(vi => vi.ClassName == entry.ClassName).ToArray();
                if (dinoSettings == null || dinoSettings.Length == 0)
                {
                    this.Add(CreateDinoSetting(entry.ClassName, GameData.MOD_UNKNOWN, false, false, true));
                }

                dinoSettings = this.Where(vi => vi.ClassName == entry.ClassName).ToArray();
                foreach (var dinoSetting in dinoSettings)
                {
                    dinoSetting.WildResistanceMultiplier = entry.Multiplier;
                }
            }

            Sort(d => d.NameSort);
        }

        public void RenderToModel()
        {
            this.DinoSpawnWeightMultipliers.Clear();
            this.PreventDinoTameClassNames.Clear();
            this.PreventDinoTameClassNames.IsEnabled = true;
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

                        var dinoSpawns = this.DinoSpawnWeightMultipliers.Where(d => d.DinoNameTag.Equals(entry.NameTag, StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (dinoSpawns == null || dinoSpawns.Length == 0)
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
                        else
                        {
                            foreach (var dinoSpawn in dinoSpawns)
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
                    }
                }

                if (entry.HasClassName && !string.IsNullOrWhiteSpace(entry.ClassName))
                {
                    if ((entry.IsTameable != DinoTamable.False) && !entry.CanTame)
                    {
                        this.PreventDinoTameClassNames.Add(entry.ClassName);
                    }

                    this.NpcReplacements.Add(new NPCReplacement() { FromClassName = entry.ClassName, ToClassName = entry.CanSpawn ? entry.ReplacementClass : string.Empty });

                    if (entry.IsTameable != DinoTamable.False)
                    {
                        // check if the value has changed.
                        if (!entry.TamedDamageMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                            this.TamedDinoClassDamageMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.TamedDamageMultiplier });

                        // check if the value has changed.
                        if (!entry.TamedResistanceMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                            this.TamedDinoClassResistanceMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.TamedResistanceMultiplier });
                    }

                    // check if the value has changed.
                    if (!entry.WildDamageMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                        this.DinoClassDamageMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.WildDamageMultiplier });

                    // check if the value has changed.
                    if (!entry.WildResistanceMultiplier.Equals(ClassMultiplier.DEFAULT_MULTIPLIER))
                        this.DinoClassResistanceMultipliers.Add(new ClassMultiplier() { ClassName = entry.ClassName, Multiplier = entry.WildResistanceMultiplier });
                }
            }
        }

        public void UpdateForLocalization()
        {
            //foreach (var dinoSetting in this)
            //{
            //    dinoSetting.FriendlyName = GameData.FriendlyNameForClass(dinoSetting.ClassName);
            //}
        }
    }
}
