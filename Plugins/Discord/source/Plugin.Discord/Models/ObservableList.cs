using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;

namespace ServerManagerTool.Plugin.Discord
{
    [DataContract]
    internal class ObservableList<T> : Bindable, IList<T>, INotifyCollectionChanged
    {
        private List<T> _listObject = null;

        public ObservableList()
            : base()
        {
            _listObject = new List<T>();
            CommitChanges();
        }

        public override bool HasAnyChanges => base.HasChanges || (_listObject?.Any(i => (i as IBindable)?.HasAnyChanges ?? false) ?? false);

        public override void CommitChanges()
        {
            base.CommitChanges();

            if (_listObject == null)
                return;

            foreach (T item in _listObject)
            {
                var bindable = item as IBindable;
                if (bindable != null)
                    bindable.CommitChanges();
            }
        }

        [DataMember]
        internal List<T> List
        {
            get => _listObject;
            set => _listObject = value;
        }

        #region IList<T>
        public T this[int index]
        {
            get => _listObject[index];
            set
            {
                T oldValue = _listObject[index];
                _listObject[index] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldValue));
            }
        }

        public int Count => _listObject.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _listObject.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Clear()
        {
            _listObject.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return _listObject.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _listObject.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _listObject.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _listObject.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _listObject.Insert(index, item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Move(int oldIndex, int newIndex)
        {
            var item = _listObject.ElementAt(oldIndex);
            if (item != null)
            {
                _listObject.Remove(item);
                _listObject.Insert(newIndex, item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
            }
        }

        public bool Remove(T item)
        {
            int index = _listObject.IndexOf(item);
            var result = _listObject.Remove(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return result;
        }

        public void RemoveAt(int index)
        {
            T item = _listObject[index];
            _listObject.RemoveAt(index);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _listObject.GetEnumerator();
        }
        #endregion

        #region INotifyCollectionChanged
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
            if (!_isUpdating)
                HasChanges = true;
        }
        #endregion
    }
}
