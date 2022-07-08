using ServerManagerTool.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class ResourceClassMultiplierList : AggregateIniValueList<ResourceClassMultiplier>
    {
        public ResourceClassMultiplierList(string aggregateValueName, Func<IEnumerable<ResourceClassMultiplier>> resetFunc)
            : base(aggregateValueName, resetFunc)
        {
        }

        public override void FromIniValues(IEnumerable<string> iniValues)
        {
            var items = iniValues?.Select(AggregateIniValue.FromINIValue<ResourceClassMultiplier>);

            Clear();
            if (this._resetFunc != null)
                this.AddRange(this._resetFunc());

            AddRange(items.Where(i => !this.Any(r => r.IsEquivalent(i))));

            foreach (var item in items.Where(i => this.Any(r => r.IsEquivalent(i))))
            {
                this.FirstOrDefault(r => r.IsEquivalent(item)).Multiplier = item.Multiplier;
            }

            IsEnabled = this.Any(d => d.ShouldSave());

            Sort(AggregateIniValue.SortKeySelector);
        }
        public override void Reset()
        {
            Clear();

            if (this._resetFunc != null)
                this.AddRange(this._resetFunc());

            IsEnabled = this.Any(d => d.ShouldSave());

            this.Sort(AggregateIniValue.SortKeySelector);
        }

        public override IEnumerable<string> ToIniValues()
        {
            if (string.IsNullOrWhiteSpace(IniCollectionKey))
                return this.Where(d => d.ShouldSave()).Select(d => d.ToINIValue());

            return this.Where(d => d.ShouldSave()).Select(d => $"{this.IniCollectionKey}={d.ToINIValue()}");
        }
    }

    [DataContract]
    public class ResourceClassMultiplier : ClassMultiplier
    {
        public static readonly DependencyProperty ModProperty = DependencyProperty.Register(nameof(Mod), typeof(string), typeof(ResourceClassMultiplier), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty KnownResourceProperty = DependencyProperty.Register(nameof(KnownResource), typeof(bool), typeof(ResourceClassMultiplier), new PropertyMetadata(false));

        [DataMember]
        public string Mod
        {
            get { return (string)GetValue(ModProperty); }
            set { SetValue(ModProperty, value); }
        }

        public bool KnownResource
        {
            get { return (bool)GetValue(KnownResourceProperty); }
            set { SetValue(KnownResourceProperty, value); }
        }

        public override string DisplayName => GameData.FriendlyResourceNameForClass(ClassName);

        public string DisplayMod => GameData.FriendlyNameForClass($"Mod_{Mod}", true) ?? Mod;

        public new static ResourceClassMultiplier FromINIValue(string iniValue)
        {
            var newSpawn = new ResourceClassMultiplier();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override string GetSortKey()
        {
            return $"{DisplayName}|Mod";
        }

        public override void InitializeFromINIValue(string value)
        {
            base.InitializeFromINIValue(value);

            if (!KnownResource)
                Mod = GameData.MOD_UNKNOWN;
        }

        public override bool ShouldSave()
        {
            if (!KnownResource)
                return true;

            var resource = GameData.GetResourceMultiplierForClass(ClassName);
            if (resource == null)
                return true;

            return (!resource.Multiplier.Equals(Multiplier));
        }
    }
}
