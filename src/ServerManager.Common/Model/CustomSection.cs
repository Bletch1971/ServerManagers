using ServerManagerTool.Common.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ServerManagerTool.Common.Model
{
    public class CustomSection : DependencyObject, IIniValuesCollection, IEnumerable<CustomItem>
    {
        public CustomSection()
        {
            SectionItems = new ObservableCollection<CustomItem>();
            Update();
        }

        public static readonly DependencyProperty IsDeletedProperty = DependencyProperty.Register(nameof(IsDeleted), typeof(bool), typeof(CustomSection), new PropertyMetadata(false));
        public bool IsDeleted
        {
            get { return (bool)GetValue(IsDeletedProperty); }
            set { SetValue(IsDeletedProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(CustomSection), new PropertyMetadata(false));
        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty SectionItemsProperty = DependencyProperty.Register(nameof(SectionItems), typeof(ObservableCollection<CustomItem>), typeof(CustomSection), new PropertyMetadata(null));
        public ObservableCollection<CustomItem> SectionItems
        {
            get { return (ObservableCollection<CustomItem>)GetValue(SectionItemsProperty); }
            set { SetValue(SectionItemsProperty, value); }
        }

        public static readonly DependencyProperty SectionNameProperty = DependencyProperty.Register(nameof(SectionName), typeof(string), typeof(CustomSection), new PropertyMetadata(string.Empty));
        public string SectionName
        {
            get { return (string)GetValue(SectionNameProperty); }
            set { SetValue(SectionNameProperty, value); }
        }

        public bool IsArray => false;

        public string IniCollectionKey => SectionName;

        public void Add(string itemKey, string itemValue)
        {
            var item = new CustomItem();
            item.ItemKey = itemKey;
            item.ItemValue = itemValue;
            SectionItems.Add(item);

            Update();
        }

        public void AddRange(IEnumerable<CustomItem> values)
        {
            foreach (var value in values)
            {
                SectionItems.Add(value);
            }

            Update();
        }

        public void Clear()
        {
            SectionItems.Clear();
        }

        public void FromIniValues(IEnumerable<string> values)
        {
            AddRange(values.Select(v => CustomItem.FromINIValue(v)));
        }

        public IEnumerator<CustomItem> GetEnumerator()
        {
            return SectionItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return SectionItems.GetEnumerator();
        }

        public bool Remove(CustomItem item)
        {
            return SectionItems.Remove(item);
        }

        public IEnumerable<string> ToIniValues()
        {
            var values = new List<string>();
            values.AddRange(SectionItems.Select(i => i.ToINIValue()).Where(i => i != null));
            return values;
        }

        public override string ToString()
        {
            return $"{SectionName}; Count={SectionItems.Count}";
        }

        public void Update()
        {
            this.IsEnabled = (!IsDeleted && SectionItems.Count != 0);
        }
    }
}
