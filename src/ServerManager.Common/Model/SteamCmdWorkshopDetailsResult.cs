using NeXt.Vdf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerManagerTool.Common.Model
{
    public class SteamCmdWorkshopDetailsResult
    {
        public static SteamCmdAppWorkshop Deserialize(VdfValue data)
        {
            var result = new SteamCmdAppWorkshop();

            var vdfTable = data as VdfTable;
            if (vdfTable != null)
            {
                var value = vdfTable.FirstOrDefault(v => v.Name.Equals("appid", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.appid = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("SizeOnDisk", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.SizeOnDisk = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("NeedsUpdate", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.NeedsUpdate = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("NeedsDownload", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.NeedsDownload = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("TimeLastUpdated", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.TimeLastUpdated = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("TimeLastAppRan", StringComparison.OrdinalIgnoreCase));
                if (value != null) result.TimeLastAppRan = GetValue(value);

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("WorkshopItemsInstalled", StringComparison.OrdinalIgnoreCase));
                var tableValue = value as VdfTable;
                if (tableValue != null && tableValue.Count > 0)
                {
                    result.WorkshopItemsInstalled = new List<SteamCmdWorkshopItemsInstalled>();

                    foreach (var item in tableValue)
                    {
                        if (item is VdfTable)
                        {
                            var temp = new SteamCmdWorkshopItemsInstalled();
                            temp.publishedfileid = item.Name;

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("manifest", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.manifest = GetValue(value);

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("size", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.size = GetValue(value);

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("timeupdated", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.timeupdated = GetValue(value);

                            result.WorkshopItemsInstalled.Add(temp);
                        }
                    }
                }

                value = vdfTable.FirstOrDefault(v => v.Name.Equals("WorkshopItemDetails", StringComparison.OrdinalIgnoreCase));
                tableValue = value as VdfTable;
                if (tableValue != null && tableValue.Count > 0)
                {
                    result.WorkshopItemDetails = new List<SteamCmdWorkshopItemDetails>();

                    foreach (var item in tableValue)
                    {
                        if (item is VdfTable)
                        {
                            var temp = new SteamCmdWorkshopItemDetails();
                            temp.publishedfileid = item.Name;

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("manifest", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.manifest = GetValue(value);

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("timeupdated", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.timeupdated = GetValue(value);

                            value = ((VdfTable)item).FirstOrDefault(v => v.Name.Equals("timetouched", StringComparison.OrdinalIgnoreCase));
                            if (value != null) temp.timetouched = GetValue(value);

                            result.WorkshopItemDetails.Add(temp);
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
