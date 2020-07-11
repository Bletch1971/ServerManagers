using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ServerManagerTool.Plugin.Discord
{
    internal class AlertTypeValueList : List<AlertTypeValue>, IBindable, INotifyCollectionChanged
    {
        private bool _hasChanges = false;

        public bool HasChanges
        {
            get => _hasChanges;
            set => _hasChanges = value;
        }

        public bool HasAnyChanges => _hasChanges || this.Any(a => a?.HasAnyChanges ?? false);

        public void BeginUpdate()
        {
        }

        public void CommitChanges()
        {
            HasChanges = false;

            foreach (var alertType in this)
            {
                alertType.CommitChanges();
            }
        }

        public void EndUpdate()
        {
        }

        #region INotifyCollectionChanged
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        public void NotifyAdd(AlertTypeValue item, bool setChanged = true)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            if (setChanged)
                HasChanges = true;
        }

        public void NotifyClear(bool setChanged = true)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (setChanged)
                HasChanges = true;
        }

        public void NotifyRemove(AlertTypeValue item, int index, bool setChanged = true)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            if (setChanged)
                HasChanges = true;
        }
    }
}
