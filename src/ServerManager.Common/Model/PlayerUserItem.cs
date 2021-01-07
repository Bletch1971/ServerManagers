using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Common.Model
{
    [DataContract]
    public class PlayerUserItem : DependencyObject
    {
        public static readonly DependencyProperty PlayerIdProperty = DependencyProperty.Register(nameof(PlayerId), typeof(string), typeof(PlayerUserItem), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty PlayerNameProperty = DependencyProperty.Register(nameof(PlayerName), typeof(string), typeof(PlayerUserItem), new PropertyMetadata(string.Empty));

        [DataMember]
        public string PlayerId
        {
            get { return (string)GetValue(PlayerIdProperty); }
            set { SetValue(PlayerIdProperty, value); }
        }

        [DataMember]
        public string PlayerName
        {
            get { return (string)GetValue(PlayerNameProperty); }
            set { SetValue(PlayerNameProperty, value); }
        }

        public static PlayerUserItem GetItem(SteamUserDetail detail)
        {
            if (string.IsNullOrWhiteSpace(detail.steamid))
                return null;

            var result = new PlayerUserItem
            {
                PlayerId = detail.steamid,
                PlayerName = detail.personaname ?? string.Empty,
            };

            return result;
        }

        public override string ToString()
        {
            return $"{PlayerId} - {PlayerName}";
        }
    }
}
