using System.Windows;

namespace ServerManagerTool.Common.Model
{
    public class CustomItem : DependencyObject
    {
        public static readonly DependencyProperty ItemKeyProperty = DependencyProperty.Register(nameof(ItemKey), typeof(string), typeof(CustomItem), new PropertyMetadata(string.Empty));
        public string ItemKey
        {
            get { return (string)GetValue(ItemKeyProperty); }
            set { SetValue(ItemKeyProperty, value); }
        }

        public static readonly DependencyProperty ItemValueProperty = DependencyProperty.Register(nameof(ItemValue), typeof(string), typeof(CustomItem), new PropertyMetadata(string.Empty));
        public string ItemValue
        {
            get { return (string)GetValue(ItemValueProperty); }
            set { SetValue(ItemValueProperty, value); }
        }

        public static CustomItem FromINIValue(string value)
        {
            var result = new CustomItem();
            result.InitializeFromINIValue(value);
            return result;
        }

        protected virtual void InitializeFromINIValue(string value)
        {
            var kvPair = value.Split(new[] { '=' }, 2);
            if (kvPair.Length > 1)
            {
                ItemKey = kvPair[0];
                ItemValue = kvPair[1];
            }
            else if (kvPair.Length > 0)
            {
                ItemKey = kvPair[0];
                ItemValue = string.Empty;
            }
        }

        public virtual string ToINIValue()
        {
            return this.ToString();
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(ItemKey))
                return null;
            return $"{ItemKey}={ItemValue}";
        }
    }
}
