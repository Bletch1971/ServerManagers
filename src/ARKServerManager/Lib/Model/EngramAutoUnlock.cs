using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    public class EngramAutoUnlockList : AggregateIniValueList<EngramAutoUnlock>
    {
        public EngramAutoUnlockList(string aggregateValueName)
            : base(aggregateValueName, null)
        {
        }

        public override void FromIniValues(IEnumerable<string> iniValues)
        {
            var items = iniValues?.Select(AggregateIniValue.FromINIValue<EngramAutoUnlock>);

            Clear();

            AddRange(items.Where(i => !this.Any(e => e.IsEquivalent(i))));

            foreach (var item in items.Where(i => this.Any(e => e.IsEquivalent(i))))
            {
                var e = this.FirstOrDefault(r => r.IsEquivalent(item));
                e.LevelToAutoUnlock = item.LevelToAutoUnlock;
            }

            IsEnabled = (Count != 0);

            Sort(AggregateIniValue.SortKeySelector);
        }

        public override IEnumerable<string> ToIniValues()
        {
            if (string.IsNullOrWhiteSpace(IniCollectionKey))
                return this.Where(d => d.ShouldSave()).Select(d => d.ToINIValue());

            return this.Where(d => d.ShouldSave()).Select(d => $"{this.IniCollectionKey}={d.ToINIValue()}");
        }
    }

    public class EngramAutoUnlock : AggregateIniValue
    {
        public static readonly DependencyProperty EngramClassNameProperty = DependencyProperty.Register(nameof(EngramClassName), typeof(string), typeof(EngramAutoUnlock), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty LevelToAutoUnlockProperty = DependencyProperty.Register(nameof(LevelToAutoUnlock), typeof(int), typeof(EngramAutoUnlock), new PropertyMetadata(0));

        [DataMember]
        [AggregateIniValueEntry]
        public string EngramClassName
        {
            get { return (string)GetValue(EngramClassNameProperty); }
            set { SetValue(EngramClassNameProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public int LevelToAutoUnlock
        {
            get { return (int)GetValue(LevelToAutoUnlockProperty); }
            set { SetValue(LevelToAutoUnlockProperty, value); }
        }

        public static EngramAutoUnlock FromINIValue(string iniValue)
        {
            var engramAutoUnlock = new EngramAutoUnlock();
            engramAutoUnlock.InitializeFromINIValue(iniValue);
            return engramAutoUnlock;
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return String.Equals(this.EngramClassName, ((EngramAutoUnlock)other).EngramClassName, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToINIValue()
        {
            return base.ToINIValue();
        }
    }
}
