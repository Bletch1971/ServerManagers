using ServerManagerTool.Common.Model;
using ServerManagerTool.Enums;
using ServerManagerTool.Lib.ViewModel;
using ServerManagerTool.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Lib
{
    public static class GameData
    {
        public const string MOD_ALL = "All";
        public const string MOD_UNKNOWN = "Unknown";

        public const string RCONINPUTMODE_COMMAND = "Command";

        public static event EventHandler GameDataLoaded;

        public static string MainDataFolder = Path.Combine(Environment.CurrentDirectory, Config.Default.GameDataDir);
        public static string UserDataFolder = Path.Combine(Config.Default.DataDir, Config.Default.GameDataDir);

        public static long DefaultMaxExperiencePointsDino = 10;
        public static long DefaultMaxExperiencePointsPlayer = 5;

        private static MainGameData gameData = null;

        public static void Initialize()
        {
            Load();
            OnGameDataLoaded();
        }

        private static void Load()
        {
            // read static game data
            GameDataUtils.ReadAllData(out gameData, MainDataFolder, Config.Default.GameDataExtension, Config.Default.GameDataApplication);

            // read user game data
            MainGameData userGameData = new MainGameData();
            if (!UserDataFolder.Equals(MainDataFolder, StringComparison.OrdinalIgnoreCase))
            {
                GameDataUtils.ReadAllData(out userGameData, UserDataFolder, Config.Default.GameDataExtension, Config.Default.GameDataApplication, true);
            }

            // creatures
            gameData.Creatures.AddRange(userGameData.Creatures);

            dinoSpawns = gameData.Creatures.ConvertAll(item => new DinoSpawn { ClassName = item.ClassName, Mod = item.Mod, KnownDino = true, DinoNameTag = item.NameTag }).ToArray();
            dinoMultipliers = gameData.Creatures.ConvertAll(item => new ClassMultiplier { ClassName = item.ClassName }).ToArray();

            // engrams
            gameData.Engrams.AddRange(userGameData.Engrams);

            engrams = gameData.Engrams.ConvertAll(item => new Engram { EngramClassName = item.ClassName, EngramLevelRequirement = item.Level, EngramPointsCost = item.Points, Mod = item.Mod, KnownEngram = true, IsTekgram = item.IsTekGram }).ToArray();

            // items
            gameData.Items.AddRange(userGameData.Items);

            items = gameData.Items.ConvertAll(item => new PrimalItem { ClassName = item.ClassName, Mod = item.Mod, KnownItem = true, Category = item.Category }).ToArray();

            // resources
            resourceMultipliers = gameData.Items.Where(item => item.IsHarvestable).Select(item => new ResourceClassMultiplier { ClassName = item.ClassName, Mod = item.Mod, KnownResource = true }).ToArray();

            // map spawners
            gameData.MapSpawners.AddRange(userGameData.MapSpawners);

            mapSpawners = gameData.MapSpawners.ConvertAll(item => new MapSpawner { ClassName = item.ClassName, Mod = item.Mod, KnownSpawner = true }).ToArray();

            // supply crates
            gameData.SupplyCrates.AddRange(userGameData.SupplyCrates);

            var crates = gameData.SupplyCrates.ConvertAll(item => new SupplyCrate { ClassName = item.ClassName, Mod = item.Mod, KnownSupplyCrate = true });

            // inventories
            gameData.Inventories.AddRange(userGameData.Inventories);

            crates.AddRange(gameData.Inventories.ConvertAll(item => new SupplyCrate { ClassName = item.ClassName, Mod = item.Mod, KnownSupplyCrate = true }));

            supplyCrates = crates.ToArray();

            // game maps
            gameData.GameMaps.AddRange(userGameData.GameMaps);

            if (gameData.GameMaps.Count > 0)
            {
                var maps1 = gameMaps.ToList();
                maps1.AddRange(gameData.GameMaps.Where(item => !item.IsSotF).Select(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));
                var maps2 = gameMapsSotF.ToList();
                maps2.AddRange(gameData.GameMaps.Where(item => item.IsSotF).Select(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));

                gameMaps = maps1.ToArray();
                gameMapsSotF = maps2.ToArray();
            }

            // total conversion mods
            gameData.TotalConversions.AddRange(userGameData.TotalConversions);

            if (gameData.TotalConversions.Count > 0)
            {
                var mods1 = totalConversions.ToList();
                mods1.AddRange(gameData.TotalConversions.Where(item => !item.IsSotF).Select(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));
                var mods2 = totalConversionsSotF.ToList();
                mods2.AddRange(gameData.TotalConversions.Where(item => item.IsSotF).Select(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));

                totalConversions = mods1.ToArray();
                totalConversionsSotF = mods2.ToArray();
            }

            // creature levels
            if (userGameData.CreatureLevels.Count > 0)
                gameData.CreatureLevels = userGameData.CreatureLevels;

            if (gameData.CreatureLevels.Count > 0)
            {
                levelsDino = gameData.CreatureLevels.ConvertAll(item => new Level { XPRequired = item.XPRequired }).ToArray();
                DefaultMaxExperiencePointsDino = levelsDino.Max(l => l.XPRequired) + 1;
            }

            // player levels
            if (userGameData.PlayerLevels.Count > 0)
                gameData.PlayerLevels = userGameData.PlayerLevels;

            LevelsPlayerAdditional = userGameData.PlayerAdditionalLevels;

            if (gameData.PlayerLevels.Count > 0)
            {
                levelsPlayer = gameData.PlayerLevels.ConvertAll(item => new Level { EngramPoints = item.EngramPoints, XPRequired = item.XPRequired }).ToArray();
                DefaultMaxExperiencePointsPlayer = levelsPlayer.Max(l => l.XPRequired) + 1;
            }

            if (gameData.PlayerAdditionalLevels > LevelsPlayerAdditional)
                LevelsPlayerAdditional = gameData.PlayerAdditionalLevels;

            // branches
            gameData.Branches.AddRange(userGameData.Branches);

            if (gameData.Branches.Count > 0)
            {
                var branches1 = branches.ToList();
                branches1.AddRange(gameData.Branches.Where(item => !item.IsSotF).Select(item => new ComboBoxItem { ValueMember = item.BranchName, DisplayMember = item.Description }));
                var branches2 = branchesSotF.ToList();
                branches2.AddRange(gameData.Branches.Where(item => item.IsSotF).Select(item => new ComboBoxItem { ValueMember = item.BranchName, DisplayMember = item.Description }));

                branches = branches1.ToArray();
                branchesSotF = branches2.ToArray();
            }

            // events
            gameData.Events.AddRange(userGameData.Events);

            if (gameData.Events.Count > 0)
            {
                var events1 = events.ToList();
                events1.AddRange(gameData.Events.Where(item => !item.IsSotF).Select(item => new ComboBoxItem { ValueMember = item.EventName, DisplayMember = item.Description }));
                var events2 = eventsSotF.ToList();
                events2.AddRange(gameData.Events.Where(item => item.IsSotF).Select(item => new ComboBoxItem { ValueMember = item.EventName, DisplayMember = item.Description }));

                events = events1.ToArray();
                eventsSotF = events2.ToArray();
            }

            // official mods
            gameData.OfficialMods.AddRange(userGameData.OfficialMods);

            if (gameData.OfficialMods.Count > 0)
            {
                ModUtils.AddOfficialMods(gameData.OfficialMods.Where(m => !string.IsNullOrWhiteSpace(m.ModId)).Select(m => m.ModId).ToList());
            }

            // rcon input modes
            gameData.RconInputModes.AddRange(userGameData.RconInputModes);

            if (gameData.RconInputModes.Count > 0)
            {
                var modes1 = rconInputModes.ToList();
                modes1.AddRange(gameData.RconInputModes.Select(item => new ComboBoxItem { ValueMember = item.Command, DisplayMember = item.Description }));

                rconInputModes = modes1.ToArray();
            }
        }

        private static void OnGameDataLoaded()
        {
            GameDataLoaded?.Invoke(null, EventArgs.Empty);
        }

        public static void Reload()
        {
            gameData = null;

            Load();
            OnGameDataLoaded();
        }

        public static string FriendlyNameForClass(string className, bool returnNullIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? (returnNullIfNotFound ? null : string.Empty) : GlobalizedApplication.Instance.GetResourceString(className) ?? (returnNullIfNotFound ? null : className);

        #region Creatures
        private static DinoSpawn[] dinoSpawns = new DinoSpawn[0];

        public static IEnumerable<DinoSpawn> GetDinoSpawns() => dinoSpawns.Select(d => d.Duplicate<DinoSpawn>());

        public static IEnumerable<NPCReplacement> GetNPCReplacements() => dinoSpawns.Select(d => new NPCReplacement() { FromClassName = d.ClassName, ToClassName = d.ClassName });

        public static bool HasCreatureForClass(string className) => !string.IsNullOrWhiteSpace(className) && dinoSpawns.Any(e => e.ClassName.Equals(className));

        public static bool IsSpawnableForClass(string className) => gameData?.Creatures?.FirstOrDefault(c => c.ClassName.Equals(className))?.IsSpawnable ?? true;

        public static DinoTamable IsTameableForClass(string className) => gameData?.Creatures?.FirstOrDefault(c => c.ClassName.Equals(className))?.IsTameable ?? DinoTamable.True;

        public static DinoBreedingable IsBreedingableForClass(string className) => gameData?.Creatures?.FirstOrDefault(c => c.ClassName.Equals(className))?.IsBreedingable ?? DinoBreedingable.True;

        public static string NameTagForClass(string className, bool returnEmptyIfNotFound = false) => gameData?.Creatures?.FirstOrDefault(c => c.ClassName.Equals(className))?.NameTag ?? (returnEmptyIfNotFound ? string.Empty : className);

        public static string FriendlyCreatureNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Creatures?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        private static ClassMultiplier[] dinoMultipliers = new ClassMultiplier[0];

        public static IEnumerable<ClassMultiplier> GetDinoMultipliers() => dinoMultipliers.Select(d => d.Duplicate<ClassMultiplier>());
        #endregion

        #region Engrams
        private static Engram[] engrams = new Engram[0];

        public static IEnumerable<Engram> GetEngrams() => engrams.Select(d => d.Duplicate());

        public static IEnumerable<EngramEntry> GetEngramEntries() => engrams.Select(d => new EngramEntry() { EngramClassName = d.EngramClassName, EngramLevelRequirement = d.EngramLevelRequirement, EngramPointsCost = d.EngramPointsCost });

        public static Engram GetEngramForClass(string className) => engrams.FirstOrDefault(e => e.EngramClassName.Equals(className));

        public static bool HasEngramForClass(string className) => !string.IsNullOrWhiteSpace(className) && engrams.Any(e => e.EngramClassName.Equals(className));

        public static bool IsTekgram(string className) => engrams.Any(e => e.EngramClassName.Equals(className) && e.IsTekgram);

        public static string FriendlyEngramNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Engrams?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Items
        private static PrimalItem[] items = new PrimalItem[0];

        public static IEnumerable<PrimalItem> GetItems() => items.Select(d => d.Duplicate());

        public static PrimalItem GetItemForClass(string className) => items.FirstOrDefault(e => e.ClassName.Equals(className));

        public static bool HasItemForClass(string className) => !string.IsNullOrWhiteSpace(className) && items.Any(e => e.ClassName.Equals(className));

        public static string FriendlyItemNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Items?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        public static string FriendlyItemModNameForClass(string className) => string.IsNullOrWhiteSpace(className) ? string.Empty : GameData.FriendlyNameForClass($"Mod_{gameData?.Items?.FirstOrDefault(i => i.ClassName.Equals(className))?.Mod}", true) ?? string.Empty;
        #endregion

        #region Resources
        private static ResourceClassMultiplier[] resourceMultipliers = new ResourceClassMultiplier[0];

        public static IEnumerable<ResourceClassMultiplier> GetResourceMultipliers() => resourceMultipliers.Select(d => d.Duplicate<ResourceClassMultiplier>());

        public static ResourceClassMultiplier GetResourceMultiplierForClass(string className) => resourceMultipliers.FirstOrDefault(e => e.ClassName.Equals(className));

        public static bool HasResourceMultiplierForClass(string className) => !string.IsNullOrWhiteSpace(className) && resourceMultipliers.Any(e => e.ClassName.Equals(className));

        public static string FriendlyResourceNameForClass(string className) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.Items?.FirstOrDefault(i => i.ClassName.Equals(className) && i.IsHarvestable)?.Description ?? className;
        #endregion

        #region Map Spawners
        private static MapSpawner[] mapSpawners = new MapSpawner[0];

        public static IEnumerable<MapSpawner> GetMapSpawners() => mapSpawners.Select(d => d.Duplicate());

        public static MapSpawner GetMapSpawnerForClass(string className) => mapSpawners.FirstOrDefault(e => e.ClassName.Equals(className));

        public static bool HasMapSpawnerForClass(string className) => !string.IsNullOrWhiteSpace(className) && mapSpawners.Any(e => e.ClassName.Equals(className));

        public static string FriendlyMapSpawnerNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.MapSpawners?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Supply Crates
        private static SupplyCrate[] supplyCrates = new SupplyCrate[0];

        public static IEnumerable<SupplyCrate> GetSupplyCrates() => supplyCrates.Select(d => d.Duplicate());

        public static SupplyCrate GetSupplyCrateForClass(string className) => supplyCrates.FirstOrDefault(e => e.ClassName.Equals(className));

        public static bool HasSupplyCrateForClass(string className) => !string.IsNullOrWhiteSpace(className) && supplyCrates.Any(e => e.ClassName.Equals(className));

        public static string FriendlySupplyCrateNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString(className) ?? gameData?.SupplyCrates?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        public static string FriendlySupplyCrateModNameForClass(string className) => string.IsNullOrWhiteSpace(className) ? string.Empty : GameData.FriendlyNameForClass($"Mod_{gameData?.SupplyCrates?.FirstOrDefault(i => i.ClassName.Equals(className))?.Mod}", true) ?? string.Empty;
        #endregion

        #region Game Maps
        private static ComboBoxItem[] gameMaps = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetGameMaps() => gameMaps.Select(d => d.Duplicate());

        public static string FriendlyMapNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("Map_" + className) ?? gameData?.GameMaps?.FirstOrDefault(i => i.ClassName.Equals(className) && !i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        private static ComboBoxItem[] gameMapsSotF = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetGameMapsSotF() => gameMapsSotF.Select(d => d.Duplicate());

        public static string FriendlyMapSotFNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("Map_" + className) ?? gameData?.GameMaps?.FirstOrDefault(i => i.ClassName.Equals(className) && i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Total Conversions
        private static ComboBoxItem[] totalConversions = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetTotalConversions() => totalConversions.Select(d => d.Duplicate());

        public static string FriendlyTotalConversionNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("TotalConv_" + className) ?? gameData?.TotalConversions?.FirstOrDefault(i => i.ClassName.Equals(className) && !i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        private static ComboBoxItem[] totalConversionsSotF = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetTotalConversionsSotF() => totalConversionsSotF.Select(d => d.Duplicate());

        public static string FriendlyTotalConversionSotFNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("TotalConv_" + className) ?? gameData?.TotalConversions?.FirstOrDefault(i => i.ClassName.Equals(className) && i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Stats Multipliers
        internal static IEnumerable<float> GetPerLevelStatsMultipliers_DinoWild()
        {
            return new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_DinoTamed()
        {
            return new float[] { 0.2f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.17f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_DinoTamedAdd()
        {
            return new float[] { 0.14f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.14f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_DinoTamedAffinity()
        {
            return new float[] { 0.44f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.44f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetBaseStatMultipliers_Player()
        {
            return new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<float> GetPerLevelStatsMultipliers_Player()
        {
            return new float[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        }

        internal static IEnumerable<int> GetPerLevelMutagenLevelBoost_DinoWild()
        {
            return new int[] { 5, 5, 0, 0, 0, 0, 0, 5, 5, 0, 0, 0 };
        }

        internal static IEnumerable<int> GetPerLevelMutagenLevelBoost_DinoTamed()
        {
            return new int[] { 1, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0 };
        }

        internal static bool[] GetStatMultiplierInclusions_DinoWildPerLevel()
        {
            return new bool[] { true, true, false, true, true, false, true, true, true, true, false, true };
        }

        internal static bool[] GetStatMultiplierInclusions_DinoTamedPerLevel()
        {
            return new bool[] { true, true, false, true, true, false, true, true, true, true, false, true };
        }

        internal static bool[] GetStatMultiplierInclusions_DinoTamedAdd()
        {
            return new bool[] { true, true, true, true, true, true, true, true, true, true, true, true };
        }

        internal static bool[] GetStatMultiplierInclusions_DinoTamedAffinity()
        {
            return new bool[] { true, true, true, true, true, true, true, true, true, true, true, true };
        }

        internal static bool[] GetStatMultiplierInclusions_PlayerBase()
        {
            return new bool[] { true, true, true, true, true, true, true, true, true, true, true, true };
        }

        internal static bool[] GetStatMultiplierInclusions_PlayerPerLevel()
        {
            return new bool[] { true, true, false, true, true, true, true, true, true, true, true, true };
        }

        internal static bool[] GetMutagenLevelBoostInclusions_DinoWild()
        {
            return new bool[] { true, true, true, true, true, true, true, true, true, true, true, true };
        }

        internal static bool[] GetMutagenLevelBoostInclusions_DinoTamed()
        {
            return new bool[] { true, true, true, true, true, true, true, true, true, true, true, true };
        }
        #endregion

        #region Levels
        private static Level[] levelsDino = new[]
        {
            new Level { XPRequired=10 },
        };

        private static Level[] levelsPlayer = new[]
        {
            new Level { XPRequired=5, EngramPoints=8 },
        };

        public static IEnumerable<Level> LevelsDino => levelsDino.Select(l => l.Duplicate());

        public static IEnumerable<Level> LevelsPlayer => levelsPlayer.Select(l => l.Duplicate());

        public static int LevelsPlayerAdditional = 0;
        #endregion

        #region Branches
        private static ComboBoxItem[] branches = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember=FriendlyNameForClass(Config.Default.DefaultServerBranchName) },
        };

        public static IEnumerable<ComboBoxItem> GetBranches() => branches.Select(d => d.Duplicate());

        public static string FriendlyBranchName(string branchName, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(branchName) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("Branch_" + branchName) ?? gameData?.Branches?.FirstOrDefault(i => i.BranchName.Equals(branchName) && !i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : branchName);

        private static ComboBoxItem[] branchesSotF = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember=FriendlyNameForClass(Config.Default.DefaultServerBranchName) },
        };

        public static IEnumerable<ComboBoxItem> GetBranchesSotF() => branchesSotF.Select(d => d.Duplicate());

        public static string FriendlyBranchSotFName(string branchName, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(branchName) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("Branch_" + branchName) ?? gameData?.Branches?.FirstOrDefault(i => i.BranchName.Equals(branchName) && i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : branchName);
        #endregion

        #region Events
        private static ComboBoxItem[] events = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember=string.Empty },
        };

        public static IEnumerable<ComboBoxItem> GetEvents() => events.Select(d => d.Duplicate());

        public static string FriendlyEventName(string eventName, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(eventName) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("Event_" + eventName) ?? gameData?.Events?.FirstOrDefault(i => i.EventName.Equals(eventName) && !i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : eventName);

        private static ComboBoxItem[] eventsSotF = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember=string.Empty },
        };

        public static IEnumerable<ComboBoxItem> GetEventsSotF() => eventsSotF.Select(d => d.Duplicate());

        public static string FriendlyEventSotFName(string eventName, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(eventName) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("Event_" + eventName) ?? gameData?.Events?.FirstOrDefault(i => i.EventName.Equals(eventName) && i.IsSotF)?.Description ?? (returnEmptyIfNotFound ? string.Empty : eventName);
        #endregion

        #region Rcon input Modes
        private static ComboBoxItem[] rconInputModes = new[]
        {
            new ComboBoxItem { ValueMember=RCONINPUTMODE_COMMAND, DisplayMember=FriendlyNameForClass($"InputMode_{RCONINPUTMODE_COMMAND}") },
        };

        public static IEnumerable<ComboBoxItem> GetAllRconInputModes() => rconInputModes.Select(m => m.Duplicate());

        public static IEnumerable<ComboBoxItem> GetMessageRconInputModes() => rconInputModes.Where(m => !m.ValueMember.Equals(RCONINPUTMODE_COMMAND, StringComparison.OrdinalIgnoreCase)).Select(m => m.Duplicate());

        public static string FriendlyRconInputModeName(string rconInputMode, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(rconInputMode) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("InputMode_" + rconInputMode) ?? gameData?.RconInputModes?.FirstOrDefault(i => i.Command.Equals(rconInputMode))?.Description ?? (returnEmptyIfNotFound ? string.Empty : rconInputMode);
        #endregion
    }
}
