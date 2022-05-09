using ConanData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ServerManagerTool.Lib.ViewModel
{
    public class PlayerInfo : INotifyPropertyChanged
    {
        public PlayerInfo()
        {
            PlayerId = string.Empty;
            PlayerName = string.Empty;
            CharacterName = string.Empty;
            IsOnline = false;
            IsAdmin = false;
            IsWhitelisted = false;
            GuildName = string.Empty;
            IsValid = true;
            LastOnline = null;
            PlayerData = null;
        }

        public string PlayerId
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
        public string PlayerName
        {
            get { return Get<string>(); }
            set
            {
                Set(value);

                PlatformNameFilterString = value?.ToLower();
            }
        }
        public string PlatformNameFilterString
        {
            get;
            private set;
        }
        public long CharacterId
        {
            get { return Get<long>(); }
            set { Set(value); }
        }
        public string CharacterName
        {
            get { return Get<string>(); }
            set
            {
                Set(value);

                CharacterNameFilterString = value?.ToLower();
            }
        }
        public string CharacterNameFilterString
        {
            get;
            private set;
        }
        public bool IsOnline
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public bool IsAdmin
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public bool IsWhitelisted
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public string GuildName
        {
            get { return Get<string>(); }
            set
            {
                Set(value);

                GuildNameFilterString = value?.ToLower();
            }
        }
        public string GuildNameFilterString
        {
            get;
            private set;
        }
        public bool IsValid
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }
        public int? LastOnline
        {
            get { return Get<int?>(); }
            set { Set(value); }
        }
        public PlayerData PlayerData
        {
            get { return Get<PlayerData>(); }
            set { Set(value); }
        }

        public void UpdateData(PlayerData playerData)
        {
            this.PlayerData = playerData;
            this.PlayerId = playerData?.PlayerId;
            this.CharacterId = playerData?.CharacterId ?? 0L;
            this.CharacterName = playerData?.CharacterName;
            this.GuildName = playerData?.Guild?.GuildName;
            //this.IsOnline = playerData?.Online ?? false;
            this.LastOnline = playerData?.LastOnline;
        }

        public void UpdatePlatformData(PlayerData playerData)
        {
            if (playerData == null)
                return;

            if (PlayerData?.PlayerName != null)
                playerData.PlayerName = PlayerData.PlayerName;
            playerData.LastPlatformUpdateUtc = PlayerData?.LastPlatformUpdateUtc ?? DateTime.MinValue;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected T Get<T>([CallerMemberName] string name = null)
        {
            object value = null;
            if (_properties?.TryGetValue(name, out value) ?? false)
                return value == null ? default : (T)value;
            return default;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void Set<T>(T value, [CallerMemberName] string name = null)
        {
            if (Equals(value, Get<T>(name)))
                return;
            if (_properties == null)
                _properties = new Dictionary<string, object>();
            _properties[name] = value;
            OnPropertyChanged(name);
        }
        #endregion
    }
}