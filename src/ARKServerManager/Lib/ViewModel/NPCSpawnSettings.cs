using ServerManagerTool.Common.Model;
using ServerManagerTool.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ServerManagerTool.Lib.ViewModel
{
    public class NPCSpawnSettingsList : SortableObservableCollection<NPCSpawnSettings>
    {
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigAddNPCSpawnEntriesContainer { get; }
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigSubtractNPCSpawnEntriesContainer { get; }
        public NPCSpawnContainerList<NPCSpawnContainer> ConfigOverrideNPCSpawnEntriesContainer { get; }

        public NPCSpawnSettingsList()
        {
            Reset();
        }

        public NPCSpawnSettingsList(
            NPCSpawnContainerList<NPCSpawnContainer> configAddNPCSpawnEntriesContainer,
            NPCSpawnContainerList<NPCSpawnContainer> configSubtractNPCSpawnEntriesContainer,
            NPCSpawnContainerList<NPCSpawnContainer> configOverrideNPCSpawnEntriesContainer)
        {
            ConfigAddNPCSpawnEntriesContainer = configAddNPCSpawnEntriesContainer;
            ConfigSubtractNPCSpawnEntriesContainer = configSubtractNPCSpawnEntriesContainer;
            ConfigOverrideNPCSpawnEntriesContainer = configOverrideNPCSpawnEntriesContainer;

            Reset();
        }

        public void Reset()
        {
            this.Clear();
        }

        public void RenderToView()
        {
            Reset();

            foreach (var entry in this.ConfigAddNPCSpawnEntriesContainer)
            {
                if (!entry.IsValid)
                    continue;

                var spawnSettings = new NPCSpawnSettings
                {
                    UniqueId = entry.UniqueId,
                    ContainerType = NPCSpawnContainerType.Add,
                    NPCSpawnEntriesContainerClassString = entry.NPCSpawnEntriesContainerClassString,
                };
                foreach (var item in entry.NPCSpawnEntries)
                {
                    spawnSettings.NPCSpawnEntrySettings.Add(new NPCSpawnEntrySettings
                    {
                        AnEntryName = item.AnEntryName,
                        NPCClassString = item.NPCsToSpawnStrings,
                        EntryWeight = item.EntryWeight,
                    });
                }

                foreach (var item in entry.NPCSpawnLimits)
                {
                    var temp = spawnSettings.NPCSpawnEntrySettings.FirstOrDefault(i => i.NPCClassString.Equals(item.NPCClassString));
                    if (temp == null)
                        continue;

                    temp.MaxPercentageOfDesiredNumToAllow = item.MaxPercentageOfDesiredNumToAllow;
                }

                this.Add(spawnSettings);
            }

            foreach (var entry in this.ConfigSubtractNPCSpawnEntriesContainer)
            {
                if (!entry.IsValid)
                    continue;

                var spawnSettings = new NPCSpawnSettings
                {
                    UniqueId = entry.UniqueId,
                    ContainerType = NPCSpawnContainerType.Subtract,
                    NPCSpawnEntriesContainerClassString = entry.NPCSpawnEntriesContainerClassString,
                };
                foreach (var item in entry.NPCSpawnEntries)
                {
                    spawnSettings.NPCSpawnEntrySettings.Add(new NPCSpawnEntrySettings
                    {
                        AnEntryName = item.AnEntryName,
                        NPCClassString = item.NPCsToSpawnStrings,
                        EntryWeight = item.EntryWeight,
                    });
                }

                foreach (var item in entry.NPCSpawnLimits)
                {
                    var temp = spawnSettings.NPCSpawnEntrySettings.FirstOrDefault(i => i.NPCClassString.Equals(item.NPCClassString));
                    if (temp == null)
                        continue;

                    temp.MaxPercentageOfDesiredNumToAllow = item.MaxPercentageOfDesiredNumToAllow;
                }

                this.Add(spawnSettings);
            }

            foreach (var entry in this.ConfigOverrideNPCSpawnEntriesContainer)
            {
                if (!entry.IsValid)
                    continue;

                var spawnSettings = new NPCSpawnSettings
                {
                    UniqueId = entry.UniqueId,
                    ContainerType = NPCSpawnContainerType.Override,
                    NPCSpawnEntriesContainerClassString = entry.NPCSpawnEntriesContainerClassString,
                };
                foreach (var item in entry.NPCSpawnEntries)
                {
                    spawnSettings.NPCSpawnEntrySettings.Add(new NPCSpawnEntrySettings
                    {
                        AnEntryName = item.AnEntryName,
                        NPCClassString = item.NPCsToSpawnStrings,
                        EntryWeight = item.EntryWeight,
                    });
                }

                foreach (var item in entry.NPCSpawnLimits)
                {
                    var temp = spawnSettings.NPCSpawnEntrySettings.FirstOrDefault(i => i.NPCClassString.Equals(item.NPCClassString));
                    if (temp == null)
                        continue;

                    temp.MaxPercentageOfDesiredNumToAllow = item.MaxPercentageOfDesiredNumToAllow;
                }

                this.Add(spawnSettings);
            }

            Update();
        }

        public void RenderToModel()
        {
            this.ConfigAddNPCSpawnEntriesContainer.Clear();
            this.ConfigSubtractNPCSpawnEntriesContainer.Clear();
            this.ConfigOverrideNPCSpawnEntriesContainer.Clear();

            this.ConfigAddNPCSpawnEntriesContainer.IsEnabled = false;
            this.ConfigSubtractNPCSpawnEntriesContainer.IsEnabled = false;
            this.ConfigOverrideNPCSpawnEntriesContainer.IsEnabled = false;

            foreach (var entry in this)
            {
                if (!entry.IsValid)
                    continue;

                var spawnContainer = new NPCSpawnContainer
                {
                    UniqueId = entry.UniqueId,
                    NPCSpawnEntriesContainerClassString = entry.NPCSpawnEntriesContainerClassString,
                };
                spawnContainer.NPCSpawnEntries.AddRange(entry.NPCSpawnEntrySettings.Where(s => s.IsValid).Select(s => new NPCSpawnEntry
                {
                    AnEntryName = string.IsNullOrWhiteSpace(s.AnEntryName) ? string.Empty : s.AnEntryName,
                    EntryWeight = s.EntryWeight,
                    NPCsToSpawnStrings = s.NPCClassString
                }));
                spawnContainer.NPCSpawnLimits.AddRange(entry.NPCSpawnEntrySettings.Where(s => s.IsValid).Select(s => new NPCSpawnLimit
                {
                    NPCClassString = s.NPCClassString,
                    MaxPercentageOfDesiredNumToAllow = s.MaxPercentageOfDesiredNumToAllow
                }));

                switch (entry.ContainerType)
                {
                    case NPCSpawnContainerType.Add:
                        this.ConfigAddNPCSpawnEntriesContainer.Add(spawnContainer);
                        this.ConfigAddNPCSpawnEntriesContainer.IsEnabled = true;
                        break;

                    case NPCSpawnContainerType.Subtract:
                        this.ConfigSubtractNPCSpawnEntriesContainer.Add(spawnContainer);
                        this.ConfigSubtractNPCSpawnEntriesContainer.IsEnabled = true;
                        break;

                    case NPCSpawnContainerType.Override:
                        this.ConfigOverrideNPCSpawnEntriesContainer.Add(spawnContainer);
                        this.ConfigOverrideNPCSpawnEntriesContainer.IsEnabled = true;
                        break;
                }
            }
        }

        public void Update(bool recursive = true)
        {
            foreach (var npcSpawn in this)
                npcSpawn.Update(recursive);
        }
    }

    public class NPCSpawnSettings : DependencyObject, IEnumerable<NPCSpawnEntrySettings>
    {
        public NPCSpawnSettings()
        {
            NPCSpawnEntrySettings = new ObservableCollection<NPCSpawnEntrySettings>();
        }

        public Guid UniqueId = Guid.NewGuid();

        public static readonly DependencyProperty ContainerTypeProperty = DependencyProperty.Register(nameof(ContainerType), typeof(NPCSpawnContainerType), typeof(NPCSpawnSettings), new PropertyMetadata(NPCSpawnContainerType.Override));
        public NPCSpawnContainerType ContainerType
        {
            get { return (NPCSpawnContainerType)GetValue(ContainerTypeProperty); }
            set
            {
                SetValue(ContainerTypeProperty, value);
                SetShowColumns();
            }
        }

        public static readonly DependencyProperty NPCSpawnEntriesContainerClassStringProperty = DependencyProperty.Register(nameof(NPCSpawnEntriesContainerClassString), typeof(string), typeof(NPCSpawnSettings), new PropertyMetadata(string.Empty));
        public string NPCSpawnEntriesContainerClassString
        {
            get { return (string)GetValue(NPCSpawnEntriesContainerClassStringProperty); }
            set { SetValue(NPCSpawnEntriesContainerClassStringProperty, value); }
        }

        public static readonly DependencyProperty NPCSpawnEntrySettingsProperty = DependencyProperty.Register(nameof(NPCSpawnEntrySettings), typeof(ObservableCollection<NPCSpawnEntrySettings>), typeof(NPCSpawnSettings), new PropertyMetadata(null));
        public ObservableCollection<NPCSpawnEntrySettings> NPCSpawnEntrySettings
        {
            get { return (ObservableCollection<NPCSpawnEntrySettings>)GetValue(NPCSpawnEntrySettingsProperty); }
            set { SetValue(NPCSpawnEntrySettingsProperty, value); }
        }

        public string DisplayName => GameData.FriendlyMapSpawnerNameForClass(NPCSpawnEntriesContainerClassString);

        public bool IsValid => !string.IsNullOrWhiteSpace(NPCSpawnEntriesContainerClassString);

        public static readonly DependencyProperty ShowEntryNameColumnProperty = DependencyProperty.Register(nameof(ShowEntryNameColumn), typeof(bool), typeof(NPCSpawnSettings), new PropertyMetadata(true));
        public bool ShowEntryNameColumn
        {
            get { return (bool)GetValue(ShowEntryNameColumnProperty); }
            set { SetValue(ShowEntryNameColumnProperty, value); }
        }

        public static readonly DependencyProperty ShowClassStringColumnProperty = DependencyProperty.Register(nameof(ShowClassStringColumn), typeof(bool), typeof(NPCSpawnSettings), new PropertyMetadata(true));
        public bool ShowClassStringColumn
        {
            get { return (bool)GetValue(ShowClassStringColumnProperty); }
            set { SetValue(ShowClassStringColumnProperty, value); }
        }

        public static readonly DependencyProperty ShowEntryWeightColumnProperty = DependencyProperty.Register(nameof(ShowEntryWeightColumn), typeof(bool), typeof(NPCSpawnSettings), new PropertyMetadata(true));
        public bool ShowEntryWeightColumn
        {
            get { return (bool)GetValue(ShowEntryWeightColumnProperty); }
            set { SetValue(ShowEntryWeightColumnProperty, value); }
        }

        public static readonly DependencyProperty ShowMaxPercentageColumnProperty = DependencyProperty.Register(nameof(ShowMaxPercentageColumn), typeof(bool), typeof(NPCSpawnSettings), new PropertyMetadata(true));
        public bool ShowMaxPercentageColumn
        {
            get { return (bool)GetValue(ShowMaxPercentageColumnProperty); }
            set { SetValue(ShowMaxPercentageColumnProperty, value); }
        }

        private void SetShowColumns()
        {
            ShowEntryNameColumn = ContainerType != NPCSpawnContainerType.Subtract;
            ShowClassStringColumn = true;
            ShowEntryWeightColumn = ContainerType != NPCSpawnContainerType.Subtract;
            ShowMaxPercentageColumn = ContainerType != NPCSpawnContainerType.Subtract;
        }

        public IEnumerator<NPCSpawnEntrySettings> GetEnumerator()
        {
            return NPCSpawnEntrySettings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return NPCSpawnEntrySettings.GetEnumerator();
        }

        public bool IsViewValid => !string.IsNullOrWhiteSpace(NPCSpawnEntriesContainerClassString) && (NPCSpawnEntrySettings?.Count ?? 0) > 0;

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(NPCSpawnSettings), new PropertyMetadata("N"));
        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update(bool recursive = true)
        {
            if (recursive && NPCSpawnEntrySettings != null)
            {
                foreach (var itemSet in NPCSpawnEntrySettings)
                    itemSet.Update();
            }

            ValidStatus = IsViewValid 
                ? (NPCSpawnEntrySettings.Any(i => i.ValidStatus == "N") 
                    ? "N" 
                    : (NPCSpawnEntrySettings.Any(i => i.ValidStatus == "W") 
                        ? "W"
                        : (GameData.HasMapSpawnerForClass(NPCSpawnEntriesContainerClassString)
                            ? "Y"
                            : "W")))
                : "N";
        }
    }

    public class NPCSpawnEntrySettings : DependencyObject
    {
        public static readonly DependencyProperty AnEntryNameProperty = DependencyProperty.Register(nameof(AnEntryName), typeof(string), typeof(NPCSpawnEntrySettings), new PropertyMetadata(string.Empty));
        public string AnEntryName
        {
            get { return (string)GetValue(AnEntryNameProperty); }
            set { SetValue(AnEntryNameProperty, value); }
        }

        public static readonly DependencyProperty NPCClassStringProperty = DependencyProperty.Register(nameof(NPCClassString), typeof(string), typeof(NPCSpawnEntrySettings), new PropertyMetadata(string.Empty));
        public string NPCClassString
        {
            get { return (string)GetValue(NPCClassStringProperty); }
            set { SetValue(NPCClassStringProperty, value); }
        }

        public static readonly DependencyProperty EntryWeightProperty = DependencyProperty.Register(nameof(EntryWeight), typeof(float), typeof(NPCSpawnEntrySettings), new PropertyMetadata(1.0f));
        public float EntryWeight
        {
            get { return (float)GetValue(EntryWeightProperty); }
            set { SetValue(EntryWeightProperty, value); }
        }

        public static readonly DependencyProperty MaxPercentageOfDesiredNumToAllowProperty = DependencyProperty.Register(nameof(MaxPercentageOfDesiredNumToAllow), typeof(float), typeof(NPCSpawnEntrySettings), new PropertyMetadata(1.0f));
        public float MaxPercentageOfDesiredNumToAllow
        {
            get { return (float)GetValue(MaxPercentageOfDesiredNumToAllowProperty); }
            set { SetValue(MaxPercentageOfDesiredNumToAllowProperty, value); }
        }

        public string DisplayName => GameData.FriendlyCreatureNameForClass(NPCClassString);

        public bool IsValid => !string.IsNullOrWhiteSpace(NPCClassString);

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(NPCSpawnEntrySettings), new PropertyMetadata("N"));
        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update()
        {
            ValidStatus = IsValid 
                ? (GameData.HasCreatureForClass(NPCClassString) 
                    ? "Y" 
                    : "W") 
                : "N";
        }
    }
}
