using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace ServerManagerTool.Plugin.Discord
{
    [DataContract]
    internal class Bindable : INotifyPropertyChanged, IBindable
    {
        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected bool _isUpdating = false;

        public Bindable()
        {
            _properties = new Dictionary<string, object>();
        }

        protected T Get<T>([CallerMemberName] string name = null)
        {
            object value = null;
            if (_properties?.TryGetValue(name, out value) ?? false)
                return value == null ? default(T) : (T)value;
            return default(T);
        }

        protected void Set<T>(T value, bool setChanged = true, [CallerMemberName] string name = null)
        {
            if (Equals(value, Get<T>(name)))
                return;
            if (_properties == null)
                _properties = new Dictionary<string, object>();
            _properties[name] = value;
            OnPropertyChanged(name);
            if (!_isUpdating && setChanged)
                HasChanges = true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region IBindable
        public bool HasChanges
        {
            get { return Get<bool>(); }
            set { Set(value, false); }
        }

        public virtual bool HasAnyChanges => HasChanges || (_properties?.Any(p => (p.Value as IBindable)?.HasAnyChanges ?? false) ?? false);

        public virtual void CommitChanges()
        {
            HasChanges = false;

            if (_properties == null)
                return;

            foreach (var property in _properties)
            {
                var bindable = property.Value as IBindable;
                if (bindable != null)
                    bindable.CommitChanges();
            }
        }

        public void BeginUpdate()
        {
            _isUpdating = true;
        }

        public void EndUpdate()
        {
            _isUpdating = false;
        }
        #endregion
    }
}
