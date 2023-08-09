using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class ExcludeItemIndicesOverrideList : AggregateIniValueList<ExcludeItemIndicesOverride>
    {
        public ExcludeItemIndicesOverrideList(string aggregateValueName)
            : base(aggregateValueName, null)
        {
        }

        public IEnumerable<string> RenderToView()
        {
            Update();

            return new List<string>();
        }

        public void RenderToModel()
        {
        }

        public void Update()
        {
            IsEnabled = this.Count > 0;

            foreach (var excludeItemIndices in this)
                excludeItemIndices.Update();
        }
    }

    [DataContract]
    public class ExcludeItemIndicesOverride : AggregateIniValue
    {
        public static readonly DependencyProperty ItemIdProperty = DependencyProperty.Register(nameof(ItemId), typeof(Int64), typeof(ExcludeItemIndicesOverride), new PropertyMetadata(0L));
        [DataMember]
        [AggregateIniValueEntry(QuotedString = false, ExcludePropertyName = true)]
        public Int64 ItemId
        {
            get { return (Int64)GetValue(ItemIdProperty); }
            set { SetValue(ItemIdProperty, value); }
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
            Int64.TryParse(kvPair[1].Trim(' '), out Int64 kvValue);

            if (kvValue <= 0)
            {
                Update();
                return;
            }

            ItemId = kvValue;
        }

        public override string ToINIValue()
        {
            return base.ToComplexINIValue(false);
        }

        public bool IsValid => (ItemId > 0);

        public override bool ShouldSave()
        {
            return IsValid;
        }

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(ExcludeItemIndicesOverride), new PropertyMetadata("N"));

        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update()
        {
            ValidStatus = IsValid  ? "Y" : "W";
        }
    }
}
