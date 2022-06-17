using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class CraftingOverrideList : AggregateIniValueList<CraftingOverride>
    {
        public CraftingOverrideList(string aggregateValueName)
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

        public void Update(bool recursive = true)
        {
            IsEnabled = this.Count > 0;

            foreach (var craftingOverride in this)
                craftingOverride.Update(recursive);
        }
    }

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

        public string DisplayName => GameData.FriendlyItemNameForClass(ItemClassString);

        public bool IsValid => !string.IsNullOrWhiteSpace(ItemClassString) && BaseCraftingResourceRequirements.Count > 0;

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(CraftingOverride), new PropertyMetadata("N"));

        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update(bool recursive = true)
        {
            if (recursive && BaseCraftingResourceRequirements != null)
            {
                foreach (var resource in BaseCraftingResourceRequirements)
                    resource.Update();
            }

            ValidStatus = IsValid 
                ? (BaseCraftingResourceRequirements.Any(i => i.ValidStatus == "N") 
                    ? "N" 
                    : (BaseCraftingResourceRequirements.Any(i => i.ValidStatus == "W") 
                        ? "W" 
                        : (GameData.HasItemForClass(ItemClassString) 
                            ? "Y" 
                            : "W"))) 
                : "N";
        }
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

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(CraftingResourceRequirement), new PropertyMetadata("N"));

        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update()
        {
            ValidStatus = IsValid 
                ? (GameData.HasItemForClass(ResourceItemTypeString) 
                    ? "Y" 
                    : "W") 
                : "N";
        }
    }
}
