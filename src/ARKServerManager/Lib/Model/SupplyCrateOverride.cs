using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Lib.ViewModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class SupplyCrateOverrideList : AggregateIniValueList<SupplyCrateOverride>
    {
        public SupplyCrateOverrideList(string aggregateValueName)
            : base(aggregateValueName, null)
        {
        }

        public IEnumerable<string> RenderToView()
        {
            List<string> errors = new List<string>();

            foreach (var supplyCrate in this)
            {
                foreach (var itemSet in supplyCrate.ItemSets)
                {
                    foreach (var itemEntry in itemSet.ItemEntries)
                    {
                        itemEntry.Items = new ObservableCollection<SupplyCrateItemEntrySettings>();

                        for (var index = 0; index < itemEntry.ItemClassStrings.Count; index++)
                        {
                            var itemsWeight = 0.0f;
                            if (index < itemEntry.ItemsWeights.Count)
                                itemsWeight = itemEntry.ItemsWeights[index];
                            else
                                errors.Add($"Missing Supply Crate Item Weight: Crate '{supplyCrate.SupplyCrateClassString}'; Set '{itemSet.SetName}'; Entry '{itemEntry.ItemEntryName}'; Item '{itemEntry.ItemClassStrings[index]}'.");

                            itemEntry.Items.Add(new SupplyCrateItemEntrySettings {
                                ItemClassString = itemEntry.ItemClassStrings[index],
                                ItemWeight = itemsWeight,
                            });
                        }
                    }
                }
            }

            Update();

            return errors;
        }

        public void RenderToModel()
        {
            foreach (var supplyCrate in this)
            {
                foreach (var itemSet in supplyCrate.ItemSets)
                {
                    foreach (var itemEntry in itemSet.ItemEntries)
                    {
                        itemEntry.ItemClassStrings = new StringIniValueList(null, null);
                        itemEntry.ItemsWeights = new FloatIniValueList(null, null);

                        foreach (var itemClass in itemEntry.Items)
                        {
                            itemEntry.ItemClassStrings.Add(itemClass.ItemClassString);
                            itemEntry.ItemsWeights.Add(itemClass.ItemWeight);
                        }
                    }
                }
            }
        }

        public void Update(bool recursive = true)
        {
            IsEnabled = this.Count > 0;

            foreach (var supplyCrate in this)
                supplyCrate.Update(recursive);
        }
    }

    [DataContract]
    public class SupplyCrateOverride : AggregateIniValue
    {
        public SupplyCrateOverride()
        {
            ItemSets = new AggregateIniValueList<SupplyCrateItemSet>(null, null);
        }

        public static readonly DependencyProperty SupplyCrateClassStringProperty = DependencyProperty.Register(nameof(SupplyCrateClassString), typeof(string), typeof(SupplyCrateOverride), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry]
        public string SupplyCrateClassString
        {
            get { return (string)GetValue(SupplyCrateClassStringProperty); }
            set { SetValue(SupplyCrateClassStringProperty, value); }
        }

        public static readonly DependencyProperty MinItemSetsProperty = DependencyProperty.Register(nameof(MinItemSets), typeof(float), typeof(SupplyCrateOverride), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MinItemSets
        {
            get { return (float)GetValue(MinItemSetsProperty); }
            set { SetValue(MinItemSetsProperty, value); }
        }

        public static readonly DependencyProperty MaxItemSetsProperty = DependencyProperty.Register(nameof(MaxItemSets), typeof(float), typeof(SupplyCrateOverride), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MaxItemSets
        {
            get { return (float)GetValue(MaxItemSetsProperty); }
            set { SetValue(MaxItemSetsProperty, value); }
        }

        public static readonly DependencyProperty NumItemSetsPowerProperty = DependencyProperty.Register(nameof(NumItemSetsPower), typeof(float), typeof(SupplyCrateOverride), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float NumItemSetsPower
        {
            get { return (float)GetValue(NumItemSetsPowerProperty); }
            set { SetValue(NumItemSetsPowerProperty, value); }
        }

        public static readonly DependencyProperty SetsRandomWithoutReplacementProperty = DependencyProperty.Register(nameof(SetsRandomWithoutReplacement), typeof(bool), typeof(SupplyCrateOverride), new PropertyMetadata(true));
        [DataMember]
        [AggregateIniValueEntry(Key = "bSetsRandomWithoutReplacement")]
        public bool SetsRandomWithoutReplacement
        {
            get { return (bool)GetValue(SetsRandomWithoutReplacementProperty); }
            set { SetValue(SetsRandomWithoutReplacementProperty, value); }
        }

        public static readonly DependencyProperty AppendItemSetsProperty = DependencyProperty.Register(nameof(AppendItemSets), typeof(bool), typeof(SupplyCrateOverride), new PropertyMetadata(false));
        [DataMember]
        [AggregateIniValueEntry(Key = "bAppendItemSets", ExcludeIfFalse = true)]
        public bool AppendItemSets
        {
            get { return (bool)GetValue(AppendItemSetsProperty); }
            set { SetValue(AppendItemSetsProperty, value); }
        }

        public static readonly DependencyProperty AppendPreventIncreasingMinMaxItemSetsProperty = DependencyProperty.Register(nameof(AppendPreventIncreasingMinMaxItemSets), typeof(bool), typeof(SupplyCrateOverride), new PropertyMetadata(false));
        [DataMember]
        [AggregateIniValueEntry(Key = "bAppendPreventIncreasingMinMaxItemSets", ExcludeIfFalse = true)]
        public bool AppendPreventIncreasingMinMaxItemSets
        {
            get { return (bool)GetValue(AppendPreventIncreasingMinMaxItemSetsProperty); }
            set { SetValue(AppendPreventIncreasingMinMaxItemSetsProperty, value); }
        }

        public static readonly DependencyProperty ItemSetsProperty = DependencyProperty.Register(nameof(ItemSets), typeof(AggregateIniValueList<SupplyCrateItemSet>), typeof(SupplyCrateOverride), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry(ValueWithinBrackets = true, ListValueWithinBrackets = true, BracketsAroundValueDelimiter = 2)]
        public AggregateIniValueList<SupplyCrateItemSet> ItemSets
        {
            get { return (AggregateIniValueList<SupplyCrateItemSet>)GetValue(ItemSetsProperty); }
            set { SetValue(ItemSetsProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override void InitializeFromINIValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Update();
                return;
            }

            var kvPair = value.Split(new[] { '=' }, 2);
            var kvValue = kvPair[1].Trim(' ');
            if (kvValue.StartsWith("("))
                kvValue = kvValue.Substring(1);
            if (kvValue.EndsWith(")"))
                kvValue = kvValue.Substring(0, kvValue.Length - 1);

            base.FromComplexINIValue(kvValue);
        }

        public override string ToINIValue()
        {
            return base.ToComplexINIValue(true);
        }

        public string DisplayName => GameData.FriendlySupplyCrateNameForClass(SupplyCrateClassString);

        public string DisplayNameFull
        {
            get
            {
                var modName = GameData.FriendlySupplyCrateModNameForClass(SupplyCrateClassString); ;
                return $"{(string.IsNullOrWhiteSpace(modName) ? string.Empty : $"({modName}) ")}{DisplayName}";
            }
        }

        public bool IsViewValid => !string.IsNullOrWhiteSpace(SupplyCrateClassString) && (ItemSets?.Count ?? 0) > 0;

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(SupplyCrateOverride), new PropertyMetadata("N"));
        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update(bool recursive = true)
        {
            if (recursive && ItemSets != null)
            {
                foreach (var itemSet in ItemSets)
                    itemSet.Update(recursive);
            }

            ValidStatus = IsViewValid 
                ? (ItemSets.Any(i => i.ValidStatus == "N") 
                    ? "N" 
                    : (ItemSets.Any(i => i.ValidStatus == "W") 
                        ? "W"
                        : (GameData.HasSupplyCrateForClass(SupplyCrateClassString)
                            ? "Y"
                            : "W")))
                : "N";
        }
    }

    [DataContract]
    public class SupplyCrateItemSet : AggregateIniValue
    {
        public SupplyCrateItemSet()
        {
            ItemEntries = new AggregateIniValueList<SupplyCrateItemSetEntry>(null, null);
        }

        public static readonly DependencyProperty SetNameProperty = DependencyProperty.Register(nameof(SetName), typeof(string), typeof(SupplyCrateItemSet), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry(ExcludeIfEmpty = true)]
        public string SetName
        {
            get { return (string)GetValue(SetNameProperty); }
            set { SetValue(SetNameProperty, value); }
        }

        public static readonly DependencyProperty MinNumItemsProperty = DependencyProperty.Register(nameof(MinNumItems), typeof(float), typeof(SupplyCrateItemSet), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MinNumItems
        {
            get { return (float)GetValue(MinNumItemsProperty); }
            set { SetValue(MinNumItemsProperty, value); }
        }

        public static readonly DependencyProperty MaxNumItemsProperty = DependencyProperty.Register(nameof(MaxNumItems), typeof(float), typeof(SupplyCrateItemSet), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MaxNumItems
        {
            get { return (float)GetValue(MaxNumItemsProperty); }
            set { SetValue(MaxNumItemsProperty, value); }
        }

        public static readonly DependencyProperty NumItemsPowerProperty = DependencyProperty.Register(nameof(NumItemsPower), typeof(float), typeof(SupplyCrateItemSet), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float NumItemsPower
        {
            get { return (float)GetValue(NumItemsPowerProperty); }
            set { SetValue(NumItemsPowerProperty, value); }
        }

        public static readonly DependencyProperty SetWeightProperty = DependencyProperty.Register(nameof(SetWeight), typeof(float), typeof(SupplyCrateItemSet), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float SetWeight
        {
            get { return (float)GetValue(SetWeightProperty); }
            set { SetValue(SetWeightProperty, value); }
        }

        public static readonly DependencyProperty ItemsRandomWithoutReplacementProperty = DependencyProperty.Register(nameof(ItemsRandomWithoutReplacement), typeof(bool), typeof(SupplyCrateItemSet), new PropertyMetadata(true));
        [DataMember]
        [AggregateIniValueEntry(Key = "bItemsRandomWithoutReplacement")]
        public bool ItemsRandomWithoutReplacement
        {
            get { return (bool)GetValue(ItemsRandomWithoutReplacementProperty); }
            set { SetValue(ItemsRandomWithoutReplacementProperty, value); }
        }

        public static readonly DependencyProperty ItemEntriesProperty = DependencyProperty.Register(nameof(ItemEntries), typeof(AggregateIniValueList<SupplyCrateItemSetEntry>), typeof(SupplyCrateItemSet), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry(ValueWithinBrackets = true, ListValueWithinBrackets = true)]
        public AggregateIniValueList<SupplyCrateItemSetEntry> ItemEntries
        {
            get { return (AggregateIniValueList<SupplyCrateItemSetEntry>)GetValue(ItemEntriesProperty); }
            set { SetValue(ItemEntriesProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override void InitializeFromINIValue(string value)
        {
            base.FromComplexINIValue(value);
        }

        public override string ToINIValue()
        {
            return base.ToComplexINIValue(false);
        }

        public string DisplayNameFull => SetName;

        public bool IsViewValid => (ItemEntries?.Count ?? 0) > 0;

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(SupplyCrateItemSet), new PropertyMetadata("N"));
        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update(bool recursive = true)
        {
            if (recursive && ItemEntries != null)
            {
                foreach (var itemEntry in ItemEntries)
                    itemEntry.Update(recursive);
            }

            ValidStatus = IsViewValid 
                ? (ItemEntries.Any(i => i.ValidStatus == "N") 
                    ? "N" 
                    : (ItemEntries.Any(i => i.ValidStatus == "W") 
                        ? "W" 
                        : "Y")) 
                : "N";
        }
    }

    [DataContract]
    public class SupplyCrateItemSetEntry : AggregateIniValue
    {
        public SupplyCrateItemSetEntry()
        {
            ItemClassStrings = new StringIniValueList(null, null);
            ItemsWeights = new FloatIniValueList(null, null);

            Items = new ObservableCollection<SupplyCrateItemEntrySettings>();
        }

        public static readonly DependencyProperty ItemEntryNameProperty = DependencyProperty.Register(nameof(ItemEntryName), typeof(string), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry(ExcludeIfEmpty = true)]
        public string ItemEntryName
        {
            get { return (string)GetValue(ItemEntryNameProperty); }
            set { SetValue(ItemEntryNameProperty, value); }
        }

        public static readonly DependencyProperty EntryWeightProperty = DependencyProperty.Register(nameof(EntryWeight), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float EntryWeight
        {
            get { return (float)GetValue(EntryWeightProperty); }
            set { SetValue(EntryWeightProperty, value); }
        }

        public static readonly DependencyProperty MinQuantityProperty = DependencyProperty.Register(nameof(MinQuantity), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MinQuantity
        {
            get { return (float)GetValue(MinQuantityProperty); }
            set { SetValue(MinQuantityProperty, value); }
        }

        public static readonly DependencyProperty MaxQuantityProperty = DependencyProperty.Register(nameof(MaxQuantity), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MaxQuantity
        {
            get { return (float)GetValue(MaxQuantityProperty); }
            set { SetValue(MaxQuantityProperty, value); }
        }

        public static readonly DependencyProperty MinQualityProperty = DependencyProperty.Register(nameof(MinQuality), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MinQuality
        {
            get { return (float)GetValue(MinQualityProperty); }
            set { SetValue(MinQualityProperty, value); }
        }

        public static readonly DependencyProperty MaxQualityProperty = DependencyProperty.Register(nameof(MaxQuality), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float MaxQuality
        {
            get { return (float)GetValue(MaxQualityProperty); }
            set { SetValue(MaxQualityProperty, value); }
        }

        public static readonly DependencyProperty ForceBlueprintProperty = DependencyProperty.Register(nameof(ForceBlueprint), typeof(bool), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(false));
        [DataMember]
        [AggregateIniValueEntry(Key = "bForceBlueprint")]
        public bool ForceBlueprint
        {
            get { return (bool)GetValue(ForceBlueprintProperty); }
            set { SetValue(ForceBlueprintProperty, value); }
        }

        public static readonly DependencyProperty ChanceToBeBlueprintOverrideProperty = DependencyProperty.Register(nameof(ChanceToBeBlueprintOverride), typeof(float), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(0.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float ChanceToBeBlueprintOverride
        {
            get { return (float)GetValue(ChanceToBeBlueprintOverrideProperty); }
            set { SetValue(ChanceToBeBlueprintOverrideProperty, value); }
        }

        public static readonly DependencyProperty ItemClassStringsProperty = DependencyProperty.Register(nameof(ItemClassStrings), typeof(StringIniValueList), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry(ValueWithinBrackets = true)]
        public StringIniValueList ItemClassStrings
        {
            get { return (StringIniValueList)GetValue(ItemClassStringsProperty); }
            set { SetValue(ItemClassStringsProperty, value); }
        }

        public static readonly DependencyProperty ItemsWeightsProperty = DependencyProperty.Register(nameof(ItemsWeights), typeof(FloatIniValueList), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry(ValueWithinBrackets = true)]
        public FloatIniValueList ItemsWeights
        {
            get { return (FloatIniValueList)GetValue(ItemsWeightsProperty); }
            set { SetValue(ItemsWeightsProperty, value); }
        }

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<SupplyCrateItemEntrySettings>), typeof(SupplyCrateItemSetEntry), new PropertyMetadata(null));
        public ObservableCollection<SupplyCrateItemEntrySettings> Items
        {
            get { return (ObservableCollection<SupplyCrateItemEntrySettings>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override void InitializeFromINIValue(string value)
        {
            base.FromComplexINIValue(value);
        }

        public override string ToINIValue()
        {
            return base.ToComplexINIValue(false);
        }

        public string DisplayNameFull => ItemEntryName;

        public float ChanceToBeBlueprint => ForceBlueprint ? 1 : ChanceToBeBlueprintOverride;

        public bool IsModelValid => ItemClassStrings.Count > 0 && ItemClassStrings.Count == ItemsWeights.Count;

        public bool IsViewValid => (Items?.Count ?? 0) > 0;

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(SupplyCrateItemSetEntry), new PropertyMetadata("N"));
        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update(bool recursive = true)
        {
            if (recursive && Items != null)
            {
                foreach (var item in Items)
                    item.Update();
            }

            ValidStatus = IsViewValid 
                ? (Items.Any(i => i.ValidStatus == "N") 
                    ? "N" 
                    : (Items.Any(i => i.ValidStatus == "W") 
                        ? "W" 
                        : "Y")) 
                : "N";
        }
    }
}
