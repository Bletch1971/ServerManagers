using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Model;
using System;
using System.Runtime.Serialization;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class NPCReplacement : AggregateIniValue
    {
        public static readonly DependencyProperty FromClassNameProperty = DependencyProperty.Register(nameof(FromClassName), typeof(string), typeof(NPCReplacement), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty ToClassNameProperty = DependencyProperty.Register(nameof(ToClassName), typeof(string), typeof(NPCReplacement), new PropertyMetadata(String.Empty));

        [DataMember]
        [AggregateIniValueEntry]
        public string FromClassName
        {
            get { return (string)GetValue(FromClassNameProperty); }
            set { SetValue(FromClassNameProperty, value); }
        }

        [DataMember]
        [AggregateIniValueEntry]
        public string ToClassName
        {
            get { return (string)GetValue(ToClassNameProperty); }
            set { SetValue(ToClassNameProperty, value); }
        }
      
        public static NPCReplacement FromINIValue(string iniValue)
        {
            var newSpawn = new NPCReplacement();
            newSpawn.InitializeFromINIValue(iniValue);
            return newSpawn;
        }

        public override string GetSortKey()
        {
            return this.FromClassName;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return String.Equals(this.FromClassName, ((NPCReplacement)other).FromClassName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool ShouldSave()
        {
            return (!String.Equals(FromClassName, ToClassName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
