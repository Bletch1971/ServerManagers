using System.Collections.Generic;
using System;
using System.Text;

namespace NeXt.Vdf
{
    /// <summary>
    /// A VdfValue that represents a table containing other VdfValues
    /// </summary>
    public sealed class VdfTable : VdfValue, IList<VdfValue>
    {
        public VdfTable(string name) : base(name) { }

        public VdfTable(string name, IEnumerable<VdfValue> values) : this(name)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            foreach(var val in values)
            {
                Add(val);
            }
        }

        private List<VdfValue> values = new List<VdfValue>();
        private Dictionary<string, VdfValue> valuelookup = new Dictionary<string,VdfValue>();

        public int IndexOf(VdfValue item)
        {
            return values.IndexOf(item);
        }

        public void Insert(int index, VdfValue item)
        {

            if(item == null)
            {
                throw new ArgumentNullException("item");
            }
            if(index < 0 || index >= values.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if(string.IsNullOrEmpty(item.Name))
            {
                throw new ArgumentException("item name cannot be empty or null");
            }
            if (ContainsName(item.Name))
            {
                throw new ArgumentException("a value with name " + item.Name + " already exists in the table");
            }


            item.Parent = this;

            values.Insert(index, item);
            valuelookup.Add(item.Name, item);
        }

        public void InsertAfter(VdfValue item, VdfValue newitem)
        {
            if(!Contains(item))
            {
                throw new ArgumentException("item needs to exist in this table", "item");
            }

            if (string.IsNullOrEmpty(newitem.Name))
            {
                throw new ArgumentException("newitem name cannot be empty or null");
            }
            if (ContainsName(newitem.Name))
            {
                throw new ArgumentException("a value with name " + newitem.Name + " already exists in the table");
            }

            int i = -1;
            for(i = 0; i < values.Count; i++)
            {
                if(values[i] == item)
                {
                    break;
                }
            }

            if(i >= 0 && i < values.Count)
            {
                if(i == values.Count -1)
                {
                    Add(newitem);
                }
                else
                {
                    Insert(i + 1, newitem);
                }
            }
        }

        public void RemoveAt(int index)
        {
            var val = values[index];
            values.RemoveAt(index);
            valuelookup.Remove(val.Name);
            
        }

        public VdfValue this[int index]
        {
            get
            {
                return values[index];
            }
            set
            {
                if(values[index].Name != value.Name)
                {
                    valuelookup.Remove(values[index].Name);
                    valuelookup.Add(value.Name, value);
                }
                else
                {
                valuelookup[value.Name] = value;
                }
                values[index] = value;
            }
        }

        public VdfValue this[string name]
        {
            get
            {
                return valuelookup[name];
            }
        }

        public void Add(VdfValue item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (string.IsNullOrEmpty(item.Name))
            {
                throw new ArgumentException("item name cannot be empty or null");
            }
            if (ContainsName(item.Name))
            {
                throw new ArgumentException("a value with name " + item.Name + " already exists in the table");
            }
            
            item.Parent = this;

            values.Add(item);
            valuelookup.Add(item.Name, item);
        }

        public void Clear()
        {
            values.Clear();
            valuelookup.Clear();
        }

        public bool Contains(VdfValue item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (string.IsNullOrEmpty(item.Name))
            {
                throw new ArgumentException("item name cannot be empty or null");
            }

            return valuelookup.ContainsKey(item.Name) && (valuelookup[item.Name] == item);
        }
        
        public bool ContainsName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name cannot be empty");
            }

            return valuelookup.ContainsKey(name);
        }

        public VdfValue GetByName(string name)
        {
            if(ContainsName(name))
            {
                return valuelookup[name];
            }
            return null;
        }

        public void CopyTo(VdfValue[] array, int arrayIndex)
        {
            values.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return values.Count; }
        }

        public bool Remove(VdfValue item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (string.IsNullOrEmpty(item.Name))
            {
                throw new ArgumentException("item name cannot be empty or null");
            }
            if(Contains(item))
            {
                valuelookup.Remove(item.Name);
                values.Remove(item);
                return true;
            }
            return false;
        }

        public IEnumerator<VdfValue> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        bool ICollection<VdfValue>.IsReadOnly
        {
            get { return false; }
        }

        public void Traverse(Func<VdfValue, bool> call)
        {
            if (call == null)
            {
                throw new ArgumentNullException("call");
            }
            foreach (var value in values)
            {
                if (!call(value))
                {
                    break;
                }
            }
        }

        public void TraverseRecursive(Func<VdfValue, bool> call)
        {
            if (call == null)
            {
                throw new ArgumentNullException("call");
            }
            foreach (var value in values)
            {
                if (value is VdfTable)
                {
                    ((VdfTable)value).TraverseRecursive(call);
                }
                else
                {
                    if (!call(value))
                    {
                        break;
                    }
                }
            }
        }
    }
}
