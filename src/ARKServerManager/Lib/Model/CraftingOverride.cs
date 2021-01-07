using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class CraftingOverride : AggregateIniValue
    {
        public CraftingOverride()
        {
            BaseCraftingResourceRequirements = new AggregateIniValueList<CraftingResourceRequirement>(null, null);
        }

        public static readonly DependencyProperty ItemClassStringProperty = DependencyProperty.Register(nameof(ItemClassString), typeof(string), typeof(CraftingOverride), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry]
        public string ItemClassString
        {
            get { return (string)GetValue(ItemClassStringProperty); }
            set { SetValue(ItemClassStringProperty, value); }
        }

        public static readonly DependencyProperty BaseCraftingResourceRequirementsProperty = DependencyProperty.Register(nameof(BaseCraftingResourceRequirements), typeof(AggregateIniValueList<CraftingResourceRequirement>), typeof(CraftingOverride), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry(ValueWithinBrackets = true, ListValueWithinBrackets = true)]
        public AggregateIniValueList<CraftingResourceRequirement> BaseCraftingResourceRequirements
        {
            get { return (AggregateIniValueList<CraftingResourceRequirement>)GetValue(BaseCraftingResourceRequirementsProperty); }
            set { SetValue(BaseCraftingResourceRequirementsProperty, value); }
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

        public string DisplayName => GameData.FriendlyItemNameForClass(ItemClassString);

        public bool IsValid => !string.IsNullOrWhiteSpace(ItemClassString) && BaseCraftingResourceRequirements.Count > 0;
    }

    [DataContract]
    public class CraftingResourceRequirement : AggregateIniValue
    {
        public static readonly DependencyProperty ResourceItemTypeStringProperty = DependencyProperty.Register(nameof(ResourceItemTypeString), typeof(string), typeof(CraftingResourceRequirement), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry]
        public string ResourceItemTypeString
        {
            get { return (string)GetValue(ResourceItemTypeStringProperty); }
            set { SetValue(ResourceItemTypeStringProperty, value); }
        }

        public static readonly DependencyProperty BaseResourceRequirementProperty = DependencyProperty.Register(nameof(BaseResourceRequirement), typeof(float), typeof(CraftingResourceRequirement), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        public float BaseResourceRequirement
        {
            get { return (float)GetValue(BaseResourceRequirementProperty); }
            set { SetValue(BaseResourceRequirementProperty, value); }
        }

        public static readonly DependencyProperty CraftingRequireExactResourceTypeProperty = DependencyProperty.Register(nameof(CraftingRequireExactResourceType), typeof(bool), typeof(CraftingResourceRequirement), new PropertyMetadata(false));
        [DataMember]
        [AggregateIniValueEntry(Key = "bCraftingRequireExactResourceType")]
        public bool CraftingRequireExactResourceType
        {
            get { return (bool)GetValue(CraftingRequireExactResourceTypeProperty); }
            set { SetValue(CraftingRequireExactResourceTypeProperty, value); }
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

        public string DisplayName => GameData.FriendlyItemNameForClass(ResourceItemTypeString);

        public bool IsValid => !string.IsNullOrWhiteSpace(ResourceItemTypeString);
    }
}
