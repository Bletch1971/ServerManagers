using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ServerManagerTool.Plugin.Discord
{
    internal class ProfileNameValueList : List<ProfileNameValue>, IBindable, INotifyCollectionChanged
    {
        private bool _hasChanges = false;

        public bool HasChanges
        {
            get => _hasChanges;
            set => _hasChanges = value;
        }

        public bool HasAnyChanges => _hasChanges || this.Any(p => p?.HasAnyChanges ?? false);

        public void BeginUpdate()
        {
        }

        public void CommitChanges()
        {
            HasChanges = false;

            foreach (var profileName in this)
            {
                profileName.CommitChanges();
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

        public void NotifyAdd(ProfileNameValue item, bool setChanged = true)
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

        public void NotifyRemove(ProfileNameValue item, int index, bool setChanged = true)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            if (setChanged)
                HasChanges = true;
        }
    }
}
