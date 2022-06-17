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
            Update();

            return new List<string>();
        }

        public void RenderToModel()
        {
        }

        public void Update()
        {
            IsEnabled = this.Count > 0;

            foreach (var preventTransfer in this)
                preventTransfer.Update();
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
            {
                Update();
                return;
            }

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

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(PreventTransferOverride), new PropertyMetadata("N"));

        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update()
        {
            ValidStatus = IsValid 
                ? (GameData.HasCreatureForClass(DinoClassString) 
                    ? "Y" 
                    : "W") 
                : "N";
        }
    }
}
