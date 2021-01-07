using ServerManagerTool.Common.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ServerManagerTool.Common.Model
{
    public class NullableValue<T> : INotifyPropertyChanged, INullableValue
    {
        public NullableValue()
        {
            HasValue = false;
            Value = default;
            DefaultValue = default;
        }

        public NullableValue(T value)
        {
            HasValue = value != null;
            Value = value;
            DefaultValue = value;
        }

        public NullableValue(bool hasValue, T value)
        {
            HasValue = hasValue;
            Value = value;
            DefaultValue = value;
        }

        public NullableValue(T value, T defaultValue)
            : this(value)
        {
            DefaultValue = defaultValue;
        }

        public NullableValue(bool hasValue, T value, T defaultValue)
            : this(hasValue, value)
        {
            DefaultValue = defaultValue;
        }

        public bool HasValue
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public T DefaultValue
        {
            get { return Get<T>(); }
            set { Set(value); }
        }

        public T Value
        {
            get { return Get<T>(); }
            set { Set(value); }
        }

        public INullableValue Clone() => new NullableValue<T>(HasValue, Value, DefaultValue);

        public bool Equals(T value) => HasValue && object.Equals(value, Value);

        public NullableValue<T> SetValue(T value)
        {
            HasValue = true;
            Value = value;
            return this;
        }

        public NullableValue<T> SetValue(bool hasValue, T value)
        {
            HasValue = hasValue;
            Value = value;
            return this;
        }

        public void SetValue(object value)
        {
            if (value != null && value is NullableValue<T>)
            {
                HasValue = ((NullableValue<T>)value).HasValue;
                Value = ((NullableValue<T>)value).Value;
                DefaultValue = ((NullableValue<T>)value).DefaultValue;
            }
        }

        public override string ToString() => HasValue ? Value.ToString() : string.Empty;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected T2 Get<T2>([CallerMemberName] string name = null)
        {
            object value = null;
            if (_properties?.TryGetValue(name, out value) ?? false)
                return value == null ? default : (T2)value;
            return default;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void Set<T2>(T2 value, [CallerMemberName] string name = null)
        {
            if (Equals(value, Get<T2>(name)))
                return;
            if (_properties == null)
                _properties = new Dictionary<string, object>();
            _properties[name] = value;
            OnPropertyChanged(name);
        }
        #endregion
    }
}
