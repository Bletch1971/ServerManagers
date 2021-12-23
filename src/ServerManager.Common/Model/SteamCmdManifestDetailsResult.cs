using NeXt.Vdf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerManagerTool.Common.Model
{
    public class SteamCmdManifestDetailsResult
    {
        public static bool ClearUserConfigBetaKeys(VdfValue data)
        {
            var updated = false;

            var vdfTable = data as VdfTable;
            if (vdfTable != null)
            {
                var value = vdfTable.FirstOrDefault(v => v.Name.Equals("UserConfig", StringComparison.OrdinalIgnoreCase));
                var tableValue = value as VdfTable;
                if (tableValue != null && tableValue.Count > 0)
                {
                    var betaKeyItems = tableValue.Where(v => v.Name.Equals("betakey", StringComparison.OrdinalIgnoreCase)).ToArray();
                    foreach (var item in betaKeyItems)
                    {
                        tableValue.Remove(item);
                        updated = true;
                    }
                }
            }

            return updated;
        }

        public static SteamCmdAppManifest Deserialize(VdfValue data)
        {
            var result = new SteamCmdAppManifest();

            var vdfTable = data as VdfTable;
            if (vdfTable != null)
            {
                var value = vdfTable.FirstOrDefault(v => v.Name.Equals("appid", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.appid = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("UserConfig", StringComparison.OrdinalIgnoreCase));
                var tableValue = value as VdfTable;
                if (tableValue != null && tableValue.Count > 0)
                {
                    result.UserConfig = new List<SteamCmdManifestUserConfig>();

                    foreach (var item in tableValue)
                    {
                        if (item is VdfTable)
                        {
                            var temp = new SteamCmdManifestUserConfig();
                            temp.betakey = item.Name;

                            result.UserConfig.Add(temp);
                        }
                    }
                }
            }

            return result;
        }

        public static string GetValue(VdfValue data)
        {
            if (data == null)
                return null;

            switch (data.Type)
            {
                case VdfValueType.Decimal:
                    return ((VdfDecimal)data).Content.ToString("G0");
                case VdfValueType.Long:
                    return ((VdfLong)data).Content.ToString("G0");
                case VdfValueType.String:
                    return ((VdfString)data).Content;
                default:
                    return null;
            }
        }
    }
}
