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
            if (ids is null)
                return new PlayerUserList();

            var result = new PlayerUserList();
            foreach (var id in ids)
            {
                result.Add(new PlayerUserItem()
                {
                    PlayerId = id,
                    PlayerName = "<not available>",
                });
            }

            if (response?.players != null)
            {
                foreach (var detail in response.players)
                {
                    var item = result.FirstOrDefault(i => i.PlayerId == detail.steamid);
                    if (item is null)
                        continue;

                    item.PlayerId = detail.steamid;
                    item.PlayerName = detail.personaname ?? string.Empty;
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
            var ids = this.Where(i => i.PlayerId.Equals(steamId, System.StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var item in ids)
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
