using ServerManagerTool.Plugin.Common;
using System.Runtime.Serialization;

namespace ServerManagerTool.Plugin.Discord
{
    [DataContract]
    internal class AlertTypeValue : Bindable
    {
        public AlertTypeValue()
            : base()
        {
            Value = AlertType.Error;
            OriginalValue = Value;
            HasChanges = false;
        }

        public AlertTypeValue(AlertType value)
            : base()
        {
            Value = value;
            OriginalValue = Value;
            HasChanges = !Value.Equals(OriginalValue);
        }

        public AlertTypeValue(AlertType value, AlertType originalValue)
            : base()
        {
            Value = value;
            OriginalValue = originalValue;
            HasChanges = !Value.Equals(OriginalValue);
        }

        [DataMember]
        public AlertType Value
        {
            get { return Get<AlertType>(); }
            set { Set(value); }
        }

        public AlertType OriginalValue
        {
            get;
            set;
        }

        public override bool HasAnyChanges => base.HasChanges && !Value.Equals(OriginalValue);

        public override void CommitChanges()
        {
            base.CommitChanges();

            OriginalValue = Value;
        }
    }
}
