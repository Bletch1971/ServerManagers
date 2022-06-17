using ServerManagerTool.Common.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    [DataContract]
    public class EngramSettingsList : SortableObservableCollection<EngramSettings>, INotifyPropertyChanged
    {
        internal EngramSettingsList()
        {
            Reset();
        }

        public EngramSettingsList(EngramEntryList overrideNamedEngramEntries, EngramAutoUnlockList engramEntryAutoUnlocks)
        {
            OverrideNamedEngramEntries = overrideNamedEngramEntries;
            EngramEntryAutoUnlocks = engramEntryAutoUnlocks;

            Reset();
        }

        public bool IsEnabled
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DataMember]
        public bool OnlyAllowSpecifiedEngrams
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public EngramEntryList OverrideNamedEngramEntries { get; }

        public EngramAutoUnlockList EngramEntryAutoUnlocks { get; }

        public EngramSettings CreateEngramSetting(string className, string mod, bool knownEngram, bool isTekgram)
        {
            return new EngramSettings()
            {
                EngramClassName = className,
                Mod = mod,
                KnownEngram = knownEngram,
                IsTekgram = isTekgram,
            };
        }

        public void Reset()
        {
            this.Clear();

            IsEnabled = false;

            var engrams = GameData.GetEngrams();
            foreach (var engram in engrams)
            {
                var engramSetting = CreateEngramSetting(engram.EngramClassName, engram.Mod, engram.KnownEngram, engram.IsTekgram);
                engramSetting.EngramLevelRequirement = engram.EngramLevelRequirement;
                engramSetting.EngramPointsCost = engram.EngramPointsCost;
                engramSetting.LevelToAutoUnlock = engram.EngramLevelRequirement;
                this.Add(engramSetting);
            }
        }

        public void RenderToView()
        {
            Reset();

            if (this.OverrideNamedEngramEntries != null)
            {
                foreach (var entry in this.OverrideNamedEngramEntries)
                {
                    if (string.IsNullOrWhiteSpace(entry.EngramClassName))
                        continue;

                    if (!this.Any(vi => vi.EngramClassName == entry.EngramClassName))
                    {
                        var engram = GameData.GetEngramForClass(entry.EngramClassName);
                        this.Add(CreateEngramSetting(entry.EngramClassName, engram?.Mod ?? GameData.MOD_UNKNOWN, engram?.KnownEngram ?? false, engram?.IsTekgram ?? false));
                    }

                    var engramSettings = this.Where(vi => vi.EngramClassName == entry.EngramClassName);
                    foreach (var engramSetting in engramSettings)
                    {
                        engramSetting.EngramLevelRequirement = entry.EngramLevelRequirement;
                        engramSetting.EngramPointsCost = entry.EngramPointsCost;
                        engramSetting.EngramHidden = entry.EngramHidden;
                        engramSetting.RemoveEngramPreReq = entry.RemoveEngramPreReq;
                        engramSetting.LevelToAutoUnlock = entry.EngramLevelRequirement;
                        engramSetting.SaveEngramOverride = true;

                        if (engramSetting.IsTekgram)
                        {
                            // always make sure that the tekgrams have default values.
                            engramSetting.EngramLevelRequirement = 0;
                            engramSetting.EngramPointsCost = 0;
                            engramSetting.LevelToAutoUnlock = 0;
                        }

                        IsEnabled = true;
                    }
                }
            }

            if (this.EngramEntryAutoUnlocks != null)
            {
                foreach (var entry in this.EngramEntryAutoUnlocks)
                {
                    if (string.IsNullOrWhiteSpace(entry.EngramClassName))
                        continue;

                    if (!this.Any(vi => vi.EngramClassName == entry.EngramClassName))
                    {
                        var engram = GameData.GetEngramForClass(entry.EngramClassName);
                        this.Add(CreateEngramSetting(entry.EngramClassName, engram?.Mod ?? GameData.MOD_UNKNOWN, engram?.KnownEngram ?? false, engram?.IsTekgram ?? false));
                    }

                    var engramSettings = this.Where(vi => vi.EngramClassName == entry.EngramClassName);
                    foreach (var engramSetting in engramSettings)
                    {
                        engramSetting.EngramAutoUnlock = true;
                        engramSetting.LevelToAutoUnlock = entry.LevelToAutoUnlock;
                        engramSetting.SaveEngramOverride = true;

                        IsEnabled = true;
                    }
                }
            }
        }

        public void RenderToModel()
        {
            if (this.OverrideNamedEngramEntries != null)
            {
                this.OverrideNamedEngramEntries.Clear();
                this.OverrideNamedEngramEntries.IsEnabled = false;
            }

            if (this.EngramEntryAutoUnlocks != null)
            {
                this.EngramEntryAutoUnlocks.Clear();
                this.EngramEntryAutoUnlocks.IsEnabled = false;
            }

            if (!IsEnabled)
                return;

            foreach (var entry in this)
            {
                if (!entry.IsValid)
                    continue;

                if (!string.IsNullOrWhiteSpace(entry.EngramClassName))
                {
                    if (this.OverrideNamedEngramEntries != null && entry.ShouldSaveEngramEntry(OnlyAllowSpecifiedEngrams))
                    {
                        this.OverrideNamedEngramEntries.Add(entry.GetEngramEntry());
                        this.OverrideNamedEngramEntries.IsEnabled = true;
                    }

                    if (this.EngramEntryAutoUnlocks != null && entry.ShouldSaveEngramAutoUnlock(OnlyAllowSpecifiedEngrams))
                    {
                        this.EngramEntryAutoUnlocks.Add(entry.GetEngramAutoUnlock());
                        this.EngramEntryAutoUnlocks.IsEnabled = true;
                    }
                }
            }
        }

        #region INotifyPropertyChanged
        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected T Get<T>([CallerMemberName] string name = null)
        {
            object value = null;
            if (_properties?.TryGetValue(name, out value) ?? false)
                return value == null ? default : (T)value;
            return default;
        }

        protected void Set<T>(T value, [CallerMemberName] string name = null)
        {
            if (Equals(value, Get<T>(name)))
                return;
            if (_properties == null)
                _properties = new Dictionary<string, object>();
            _properties[name] = value;
            OnPropertyChanged(new PropertyChangedEventArgs(name));
        }
        #endregion
    }

    [DataContract]
    public class EngramSettings : DependencyObject
    {
        public static readonly DependencyProperty EngramClassNameProperty = DependencyProperty.Register(nameof(EngramClassName), typeof(string), typeof(EngramSettings), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ModProperty = DependencyProperty.Register(nameof(Mod), typeof(string), typeof(EngramSettings), new PropertyMetadata(GameData.MOD_UNKNOWN));
        public static readonly DependencyProperty KnownEngramProperty = DependencyProperty.Register(nameof(KnownEngram), typeof(bool), typeof(EngramSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty IsTekgramProperty = DependencyProperty.Register(nameof(IsTekgram), typeof(bool), typeof(EngramSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty EngramLevelRequirementProperty = DependencyProperty.Register(nameof(EngramLevelRequirement), typeof(int), typeof(EngramSettings), new PropertyMetadata(0));
        public static readonly DependencyProperty EngramPointsCostProperty = DependencyProperty.Register(nameof(EngramPointsCost), typeof(int), typeof(EngramSettings), new PropertyMetadata(0));
        public static readonly DependencyProperty EngramHiddenProperty = DependencyProperty.Register(nameof(EngramHidden), typeof(bool), typeof(EngramSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty RemoveEngramPreReqProperty = DependencyProperty.Register(nameof(RemoveEngramPreReq), typeof(bool), typeof(EngramSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty EngramAutoUnlockProperty = DependencyProperty.Register(nameof(EngramAutoUnlock), typeof(bool), typeof(EngramSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty LevelToAutoUnlockProperty = DependencyProperty.Register(nameof(LevelToAutoUnlock), typeof(int), typeof(EngramSettings), new PropertyMetadata(0));
        public static readonly DependencyProperty SaveEngramOverrideProperty = DependencyProperty.Register(nameof(SaveEngramOverride), typeof(bool), typeof(EngramSettings), new PropertyMetadata(false));

        [DataMember]
        public string EngramClassName
        {
            get { return (string)GetValue(EngramClassNameProperty); }
            set { SetValue(EngramClassNameProperty, value); }
        }

        [DataMember]
        public string Mod
        {
            get { return (string)GetValue(ModProperty); }
            set { SetValue(ModProperty, value); }
        }

        public bool KnownEngram
        {
            get { return (bool)GetValue(KnownEngramProperty); }
            set { SetValue(KnownEngramProperty, value); }
        }

        [DataMember]
        public bool IsTekgram
        {
            get { return (bool)GetValue(IsTekgramProperty); }
            set { SetValue(IsTekgramProperty, value); }
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(EngramClassName);

        [DataMember]
        public int EngramLevelRequirement
        {
            get { return (int)GetValue(EngramLevelRequirementProperty); }
            set { SetValue(EngramLevelRequirementProperty, value); }
        }

        [DataMember]
        public int EngramPointsCost
        {
            get { return (int)GetValue(EngramPointsCostProperty); }
            set { SetValue(EngramPointsCostProperty, value); }
        }

        [DataMember]
        public bool EngramHidden
        {
            get { return (bool)GetValue(EngramHiddenProperty); }
            set { SetValue(EngramHiddenProperty, value); }
        }

        [DataMember]
        public bool RemoveEngramPreReq
        {
            get { return (bool)GetValue(RemoveEngramPreReqProperty); }
            set { SetValue(RemoveEngramPreReqProperty, value); }
        }

        [DataMember]
        public bool EngramAutoUnlock
        {
            get { return (bool)GetValue(EngramAutoUnlockProperty); }
            set { SetValue(EngramAutoUnlockProperty, value); }
        }

        [DataMember]
        public int LevelToAutoUnlock
        {
            get { return (int)GetValue(LevelToAutoUnlockProperty); }
            set { SetValue(LevelToAutoUnlockProperty, value); }
        }

        [DataMember]
        public bool SaveEngramOverride
        {
            get { return (bool)GetValue(SaveEngramOverrideProperty); }
            set { SetValue(SaveEngramOverrideProperty, value); }
        }

        public string DisplayName => GameData.FriendlyEngramNameForClass(EngramClassName);

        public string DisplayMod => GameData.FriendlyNameForClass($"Mod_{Mod}", true) ?? Mod;

        public bool ShouldSaveEngramEntry(bool onlyAllowSpecifiedEngrams)
        {
            if (onlyAllowSpecifiedEngrams)
                return SaveEngramOverride;

            var engramEntry = GameData.GetEngramForClass(EngramClassName);
            if (engramEntry == null)
                engramEntry = new Engram();

            var engramLevelRequirement = IsTekgram ? 0 : EngramLevelRequirement;
            var engramPointsCost = IsTekgram ? 0 : EngramPointsCost;
            var engramHidden = EngramHidden;
            var removeEngramPreReq = RemoveEngramPreReq;

            return (!engramLevelRequirement.Equals(engramEntry.EngramLevelRequirement) ||
                !engramPointsCost.Equals(engramEntry.EngramPointsCost) ||
                engramHidden ||
                removeEngramPreReq);
        }

        public bool ShouldSaveEngramAutoUnlock(bool onlyAllowSpecifiedEngrams)
        {
            if (onlyAllowSpecifiedEngrams && !SaveEngramOverride)
                return false;

            return EngramAutoUnlock;
        }

        public EngramEntry GetEngramEntry()
        {
            var engramEntry = new EngramEntry() { EngramClassName = this.EngramClassName, EngramLevelRequirement = this.EngramLevelRequirement, EngramPointsCost = this.EngramPointsCost, EngramHidden = this.EngramHidden, RemoveEngramPreReq = this.RemoveEngramPreReq };
            if (IsTekgram)
            {
                // always make sure that the tekgrams have default values.
                engramEntry.EngramLevelRequirement = 0;
                engramEntry.EngramPointsCost = 0;
            }
            return engramEntry;
        }

        public EngramAutoUnlock GetEngramAutoUnlock()
        {
            var engramEntry = new EngramAutoUnlock() { EngramClassName = this.EngramClassName, LevelToAutoUnlock = this.LevelToAutoUnlock };
            return engramEntry;
        }

        #region Sort Properties
        public string SaveEngramOverrideSort => $"{SaveEngramOverride}|{IsTekgram}|{DisplayName}|{Mod}";
        public string NameSort => $"{DisplayName}|{Mod}";
        public string ModSort => $"{Mod}|{IsTekgram}|{DisplayName}";
        public string IsTekgramSort => $"{IsTekgram}|{DisplayName}|{Mod}";
        public string EngramLevelRequirementSort => $"{EngramLevelRequirement:0000000000}|{IsTekgram}|{DisplayName}|{Mod}";
        public string EngramPointsCostSort => $"{EngramPointsCost:0000000000}|{IsTekgram}|{DisplayName}|{Mod}";
        public string EngramHiddenSort => $"{EngramHidden}|{IsTekgram}|{DisplayName}|{Mod}";
        public string RemoveEngramPreReqSort => $"{RemoveEngramPreReq}|{IsTekgram}|{DisplayName}|{Mod}";
        public string EngramAutoUnlockSort => $"{EngramAutoUnlock}|{IsTekgram}|{DisplayName}|{Mod}";
        public string LevelToAutoUnlockSort => $"{LevelToAutoUnlock:0000000000}|{IsTekgram}|{DisplayName}|{Mod}";
        #endregion
    }
}
