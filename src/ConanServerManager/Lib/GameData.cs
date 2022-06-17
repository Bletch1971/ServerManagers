using ServerManagerTool.Common.Model;
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
        public const string RCONINPUTMODE_COMMAND = "Command";

        public static event EventHandler GameDataLoaded;

        public static string MainDataFolder = Path.Combine(Environment.CurrentDirectory, Config.Default.GameDataRelativePath);
        public static string UserDataFolder = Path.Combine(Config.Default.DataPath, Config.Default.GameDataRelativePath);

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

            // game maps
            gameData.GameMaps.AddRange(userGameData.GameMaps);

            if (gameData.GameMaps.Count > 0)
            {
                var maps = gameMaps.ToList();
                maps.AddRange(gameData.GameMaps.ConvertAll(item => new ComboBoxItem { ValueMember = item.ClassName, DisplayMember = item.Description }));

                gameMaps = maps.ToArray();
            }

            // branches
            gameData.Branches.AddRange(userGameData.Branches);

            if (gameData.Branches.Count > 0)
            {
                var allBranches = branches.ToList();
                allBranches.AddRange(gameData.Branches.ConvertAll(item => new ComboBoxItem { ValueMember = item.BranchName, DisplayMember = item.Description }));

                branches = allBranches.ToArray();
            }

            // server regions
            gameData.ServerRegions.AddRange(userGameData.ServerRegions);

            if (gameData.ServerRegions.Count > 0)
            {
                var allServerRegions = serverRegions.ToList();
                allServerRegions.AddRange(gameData.ServerRegions.ConvertAll(item => new ComboBoxItem { ValueMember = item.RegionNumber, DisplayMember = item.Description }));

                serverRegions = allServerRegions.ToArray();
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

        #region Game Maps
        private static ComboBoxItem[] gameMaps = new ComboBoxItem[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember="" },
        };

        public static IEnumerable<ComboBoxItem> GetGameMaps() => gameMaps.Select(m => m.Duplicate());

        public static string FriendlyMapNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("Map_" + className) ?? gameData?.GameMaps?.FirstOrDefault(i => i.ClassName.Equals(className))?.Description ?? (returnEmptyIfNotFound ? string.Empty : className);

        public static string MapSaveNameForClass(string className, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(className) ? string.Empty : gameData?.GameMaps?.FirstOrDefault(i => i.ClassName.Equals(className))?.SaveFileName ?? (returnEmptyIfNotFound ? string.Empty : className);
        #endregion

        #region Branches
        private static ComboBoxItem[] branches = new[]
        {
            new ComboBoxItem { ValueMember="", DisplayMember=FriendlyNameForClass(Config.Default.DefaultServerBranchName) },
        };

        public static IEnumerable<ComboBoxItem> GetBranches() => branches.Select(d => d.Duplicate());

        public static string FriendlyBranchName(string branchName, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(branchName) ? string.Empty : GlobalizedApplication.Instance.GetResourceString("Branch_" + branchName) ?? gameData?.Branches?.FirstOrDefault(i => i.BranchName.Equals(branchName))?.Description ?? (returnEmptyIfNotFound ? string.Empty : branchName);
        #endregion

        #region Server Regions
        private static ComboBoxItem[] serverRegions = new ComboBoxItem[0];

        public static IEnumerable<ComboBoxItem> GetServerRegions() => serverRegions.Select(d => d.Duplicate());

        public static string FriendlyServerRegionName(string regionNumber, bool returnEmptyIfNotFound = false) => string.IsNullOrWhiteSpace(regionNumber) ? string.Empty : GlobalizedApplication.Instance.GetResourceString($"ServerRegion_{regionNumber}") ?? gameData?.ServerRegions?.FirstOrDefault(i => i.RegionNumber.Equals(regionNumber))?.Description ?? (returnEmptyIfNotFound ? string.Empty : regionNumber);
        #endregion

        #region Rcon Message Modes
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
