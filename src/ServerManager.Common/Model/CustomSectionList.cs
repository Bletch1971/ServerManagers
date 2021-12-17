using ServerManagerTool.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerManagerTool.Common.Model
{
    public class CustomList : SortableObservableCollection<CustomSection>, IIniSectionCollection
    {
        public IIniValuesCollection[] Sections
        {
            get
            {
                return this.ToArray();
            }
        }

        public void Add(string sectionName, IEnumerable<string> values)
        {
            Add(sectionName, values, true);
        }

        public void Add(string sectionName, IEnumerable<string> values, bool clearExisting)
        {
            var section = this.Items.FirstOrDefault(s => s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase) && !s.IsDeleted);
            if (section == null)
            {
                section = new CustomSection();
                section.SectionName = sectionName;

                this.Add(section);
            }

            if (clearExisting)
                section.Clear();
            section.FromIniValues(values);
        }

        public new void Clear()
        {
            foreach (var section in this)
            {
                section.IsDeleted = true;
            }
            Update();
        }

        public new void Remove(CustomSection item)
        {
            if (item != null)
                item.IsDeleted = true;
            Update();
        }

        public override string ToString()
        {
            return $"Count={Count}";
        }

        public void Update()
        {
            foreach (var section in this)
            {
                section.Update();
            }

            this.Sort(s => s.SectionName);
        }
    }
}
