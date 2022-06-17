using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class StackSizeOverrideList : AggregateIniValueList<StackSizeOverride>
    {
        public StackSizeOverrideList(string aggregateValueName)
            : base(aggregateValueName, null)
        {
        }

        public IEnumerable<string> RenderToView()
        {
            List<string> errors = new List<string>();

            foreach (var stackSize in this)
            {
                if (stackSize.Quantity != null)
                {
                    stackSize.IgnoreMultiplier = stackSize.Quantity.IgnoreMultiplier;
                    stackSize.MaxItemQuantity = stackSize.Quantity.MaxItemQuantity;
                }
            }

            Update();

            return errors;
        }

        public void RenderToModel()
        {
            foreach (var stackSize in this)
            {
                if (stackSize.Quantity != null)
                {
                    stackSize.Quantity.IgnoreMultiplier = stackSize.IgnoreMultiplier;
                    stackSize.Quantity.MaxItemQuantity = stackSize.MaxItemQuantity;
                }
            }
        }

        public void Update()
        {
            IsEnabled = this.Count > 0;

            foreach (var stackSize in this)
                stackSize.Update();
        }
    }

    [DataContract]
    public class StackSizeOverride : AggregateIniValue
    {
        public StackSizeOverride()
        {
            Quantity = new StackSizeQuantity();
        }

        public static readonly DependencyProperty ItemClassStringProperty = DependencyProperty.Register(nameof(ItemClassString), typeof(string), typeof(StackSizeOverride), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry]
        public string ItemClassString
        {
            get { return (string)GetValue(ItemClassStringProperty); }
            set { SetValue(ItemClassStringProperty, value); }
        }

        public static readonly DependencyProperty IgnoreMultiplierProperty = DependencyProperty.Register(nameof(IgnoreMultiplier), typeof(bool), typeof(StackSizeOverride), new PropertyMetadata(true));
        public bool IgnoreMultiplier
        {
            get { return (bool)GetValue(IgnoreMultiplierProperty); }
            set { SetValue(IgnoreMultiplierProperty, value); }
        }

        public static readonly DependencyProperty MaxItemQuantityProperty = DependencyProperty.Register(nameof(MaxItemQuantity), typeof(int), typeof(StackSizeOverride), new PropertyMetadata(1));
        public int MaxItemQuantity
        {
            get { return (int)GetValue(MaxItemQuantityProperty); }
            set { SetValue(MaxItemQuantityProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public StackSizeQuantity Quantity
        {
            get;
            set;
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

            if (Quantity != null)
            {
                IgnoreMultiplier = Quantity.IgnoreMultiplier;
                MaxItemQuantity = Quantity.MaxItemQuantity;
            }
        }

        public override string ToINIValue()
        {
            if (Quantity != null)
            {
                Quantity.IgnoreMultiplier = IgnoreMultiplier;
                Quantity.MaxItemQuantity = MaxItemQuantity;
            }

            return base.ToComplexINIValue(true);
        }

        public string DisplayName => GameData.FriendlyItemNameForClass(ItemClassString);

        public bool IsValid => !string.IsNullOrWhiteSpace(ItemClassString);

        public override bool ShouldSave()
        {
            return IsValid;
        }

        public static readonly DependencyProperty ValidStatusProperty = DependencyProperty.Register(nameof(ValidStatus), typeof(string), typeof(StackSizeOverride), new PropertyMetadata("N"));

        public string ValidStatus
        {
            get { return (string)GetValue(ValidStatusProperty); }
            set { SetValue(ValidStatusProperty, value); }
        }

        public void Update()
        {
            ValidStatus = IsValid 
                ? (GameData.HasItemForClass(ItemClassString) 
                    ? "Y" 
                    : "W") 
                : "N";
        }
    }

    [DataContract]
    public class StackSizeQuantity : AggregateIniValue
    {
        [DataMember]
        [AggregateIniValueEntry]
        public int MaxItemQuantity
        {
            get;
            set;
        }

        [DataMember]
        [AggregateIniValueEntry(Key = "bIgnoreMultiplier")]
        public bool IgnoreMultiplier
        {
            get;
            set;
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

            var kvValue = value;
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
    }
}
