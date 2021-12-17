using ServerManagerTool.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerManagerTool.Common.Model
{
    public abstract class IniValueList<T> : SortableObservableCollection<T>, IIniValuesCollection
    {
        private bool _isEnabled;

        protected IniValueList(string aggregateValueName, Func<IEnumerable<T>> resetFunc, Func<T, T, bool> equivalencyFunc, Func<T, object> sortKeySelectorFunc, Func<T, string> toIniValue, Func<string, T> fromIniValue)
        {
            this.ToIniValue = toIniValue;
            this.FromIniValue = fromIniValue;
            this.ResetFunc = resetFunc;
            this.EquivalencyFunc = equivalencyFunc;
            this.SortKeySelectorFunc = sortKeySelectorFunc;
            this.IniCollectionKey = aggregateValueName;

            this.Reset();
            this.IsEnabled = false;
        }

        public Func<T, string> ToIniValue { get; }
        public Func<string, T> FromIniValue { get; }
        protected Func<IEnumerable<T>> ResetFunc { get; }
        public Func<T, T, bool> EquivalencyFunc { get; private set; }
        protected Func<T, object> SortKeySelectorFunc { get; }

        public bool IsEnabled
        {
            get { return this._isEnabled; }
            set
            {
                this._isEnabled = value;
                OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }

        public abstract bool IsArray { get; }

        public string IniCollectionKey { get; }

        public void AddRange(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                base.Add(value);
            }
        }

        public virtual void FromIniValues(IEnumerable<string> values)
        {
            this.Clear();

            if (this.IsArray)
            {
                var list = new List<T>();
                if (this.ResetFunc != null)
                    list.AddRange(this.ResetFunc());

                foreach(var v in values)
                {
                    var indexStart = v.IndexOf('[');
                    var indexEnd = v.IndexOf(']');

                    if(indexStart >= indexEnd)
                    {
                        // Invalid format
                        continue;
                    }

                    int index;
                    if(!int.TryParse(v.Substring(indexStart + 1, indexEnd - indexStart - 1), out index))
                    {
                        // Invalid index
                        continue;
                    }

                    if(index >= list.Count)
                    {
                        // Unexpected size
                        continue;
                    }

                    list[index] = this.FromIniValue(v.Substring(v.IndexOf('=') + 1).Trim());
                    this.IsEnabled = true;
                }

                this.AddRange(list);
            }
            else
            {
                this.AddRange(values.Select(v => v.Substring(v.IndexOf('=') + 1)).Select(this.FromIniValue));
                this.IsEnabled = (this.Count != 0);

                // Add any default values which were missing
                if (this.ResetFunc != null)
                {
                    this.AddRange(this.ResetFunc().Where(r => !this.Any(v => this.EquivalencyFunc(v, r))));
                    this.Sort(this.SortKeySelectorFunc);
                }
            }            
        }

        public virtual IEnumerable<string> ToIniValues()
        {
            var values = new List<string>();
            if (this.IsArray)
            {
                for(var i = 0; i < this.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(IniCollectionKey))
                        values.Add(this.ToIniValue(this[i]));
                    else
                        values.Add($"{this.IniCollectionKey}[{i}]={this.ToIniValue(this[i])}");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(IniCollectionKey))
                    values.AddRange(this.Select(d => this.ToIniValue(d)));
                else
                    values.AddRange(this.Select(d => $"{this.IniCollectionKey}={this.ToIniValue(d)}"));
            }
            return values;
        }

        public void Reset()
        {
            this.Clear();
            if (this.ResetFunc != null)
                this.AddRange(this.ResetFunc());
        }
    }
}
