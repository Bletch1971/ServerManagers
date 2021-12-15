using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace ServerManagerTool.Common.Model
{
    [DataContract]
    public class PlayerUserList : ObservableCollection<PlayerUserItem>
    {
        public void AddRange(PlayerUserList list)
        {
            if (list == null)
                return;

            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item?.PlayerId))
                    continue;

                if (!this.Any(i => i.PlayerId.Equals(item.PlayerId)))
                    this.Add(item);
            }
        }

        public static PlayerUserList GetList(SteamUserDetailResponse response, IEnumerable<string> ids)
        {
            var result = new PlayerUserList();
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    result.Add(new PlayerUserItem()
                    {
                        PlayerId = id,
                        PlayerName = "<not available>",
                    });
                }
            }

            if (response?.players != null)
            {
                foreach (var detail in response.players)
                {
                    var item = result.FirstOrDefault(i => i.PlayerId == detail.steamid);
                    if (item == null)
                    {
                        var newItem = PlayerUserItem.GetItem(detail);
                        if (!string.IsNullOrWhiteSpace(newItem?.PlayerId))
                            result.Add(newItem);
                    }
                    else
                    {
                        item.PlayerId = detail.steamid;
                        item.PlayerName = detail.personaname ?? string.Empty;
                    }
                }
            }

            // remove all NULL records.
            for(int index = result.Count - 1; index >= 0; index--)
            {
                if (result[index] == null)
                    result.RemoveAt(index);
            }

            return result;
        }

        public void Remove(string steamId)
        {
            foreach (var item in this.Where(i => i.PlayerId.Equals(steamId, System.StringComparison.OrdinalIgnoreCase)))
            {
                this.Remove(item);
            }
        }

        public IEnumerable<string> ToEnumerable()
        {
            return this.Select(i => i.PlayerId);
        }

        public string ToDelimitedString(string delimiter)
        {
            return string.Join(delimiter, this.Select(i => i.PlayerId));
        }

        public override string ToString()
        {
            return $"{nameof(PlayerUserList)} - {Count}";
        }
    }
}
