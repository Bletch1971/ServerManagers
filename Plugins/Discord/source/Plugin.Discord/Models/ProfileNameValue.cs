using System.Runtime.Serialization;

namespace ServerManagerTool.Plugin.Discord
{
    [DataContract]
    internal class ProfileNameValue : Bindable
    {
        public ProfileNameValue()
             : base()
        {
            Value = string.Empty;
            OriginalValue = Value;
            HasChanges = false;
        }

        public ProfileNameValue(string value)
            : base()
        {
            Value = value;
            OriginalValue = Value;
            HasChanges = !Value.Equals(OriginalValue);
        }

        public ProfileNameValue(string value, string originalValue)
            : base()
        {
            Value = value;
            OriginalValue = originalValue;
            HasChanges = !Value.Equals(OriginalValue);
        }

        [DataMember]
        public string Value
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string OriginalValue
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
