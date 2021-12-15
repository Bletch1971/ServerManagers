using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class PreventTransferOverrideList : AggregateIniValueList<PreventTransferOverride>
    {
        public PreventTransferOverrideList(string aggregateValueName)
            : base(aggregateValueName, null)
        {
        }

        public IEnumerable<string> RenderToView()
        {
            return new List<string>();
        }

        public void RenderToModel()
        {
        }

        public void UpdateForLocalization()
        {
        }
    }

    [DataContract]
    public class PreventTransferOverride : AggregateIniValue
    {
        public static readonly DependencyProperty DinoClassStringProperty = DependencyProperty.Register(nameof(DinoClassString), typeof(string), typeof(PreventTransferOverride), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry(QuotedString = false, ExcludePropertyName = true)]
        public string DinoClassString
        {
            get { return (string)GetValue(DinoClassStringProperty); }
            set { SetValue(DinoClassStringProperty, value); }
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
                return;

            var kvPair = value.Split(new[] { '=' }, 2);
            var kvValue = kvPair[1].Trim(' ');

            DinoClassString = kvValue;
        }

        public override string ToINIValue()
        {
            return base.ToComplexINIValue(false);
        }

        public string DisplayName => GameData.FriendlyItemNameForClass(DinoClassString);

        public bool IsValid => !string.IsNullOrWhiteSpace(DinoClassString);

        public override bool ShouldSave()
        {
            return IsValid;
        }
    }
}
