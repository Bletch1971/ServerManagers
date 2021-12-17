using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Common.Interfaces;
using ServerManagerTool.Common.Model;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.Enums;
using ServerManagerTool.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

namespace ServerManagerTool.Lib
{
    [DataContract]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NPCSpawnAttribute : Attribute
    {
        /// <summary>
        /// Attribute for the NPCSpawn value
        /// </summary>
        public NPCSpawnAttribute(NPCSpawnContainerType[] containerTypes)
        {
            this.ContainerTypes = containerTypes.ToList();
        }

        /// <summary>
        /// The ContainerTypes that are valid for the property.
        /// </summary>
        [DataMember]
        public List<NPCSpawnContainerType> ContainerTypes;
    }

    [DataContract]
    public class NPCSpawnContainerList<T> : AggregateIniValueList<T>, ISpawnIniValuesCollection
         where T : AggregateIniValue, new()
    {
        public NPCSpawnContainerList(string aggregateValueName, NPCSpawnContainerType containerType)
            : base(aggregateValueName, null)
        {
            ContainerType = containerType;
        }

        [DataMember]
        public NPCSpawnContainerType ContainerType
        {
            get;
            set;
        }

        public override IEnumerable<string> ToIniValues()
        {
            return this.ToIniValues(ContainerType);
        }

        public IEnumerable<string> ToIniValues(NPCSpawnContainerType containerType)
        {
            if (string.IsNullOrWhiteSpace(IniCollectionKey))
                return this.Where(d => d.ShouldSave()).Cast<ISpawnIniValue>().Select(d => d.ToIniValue(containerType));

            return this.Where(d => d.ShouldSave()).Cast<ISpawnIniValue>().Select(d => $"{this.IniCollectionKey}={d.ToIniValue(containerType)}");
        }
    }

    [DataContract]
    public class NPCSpawnContainer : AggregateIniValue, ISpawnIniValue
    {
        public NPCSpawnContainer()
        {
            NPCSpawnEntries = new NPCSpawnList<NPCSpawnEntry>(null);
            NPCSpawnLimits = new NPCSpawnList<NPCSpawnLimit>(null);
        }

        [DataMember]
        public Guid UniqueId = Guid.NewGuid();

        public static readonly DependencyProperty NPCSpawnEntriesContainerClassStringProperty = DependencyProperty.Register(nameof(NPCSpawnEntriesContainerClassString), typeof(string), typeof(NPCSpawnContainer), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry]
        [NPCSpawn(new[] { NPCSpawnContainerType.Add, NPCSpawnContainerType.Subtract, NPCSpawnContainerType.Override })]
        public string NPCSpawnEntriesContainerClassString
        {
            get { return (string)GetValue(NPCSpawnEntriesContainerClassStringProperty); }
            set { SetValue(NPCSpawnEntriesContainerClassStringProperty, value); }
        }

        public static readonly DependencyProperty NPCSpawnEntriesProperty = DependencyProperty.Register(nameof(NPCSpawnEntries), typeof(NPCSpawnList<NPCSpawnEntry>), typeof(NPCSpawnContainer), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry(ValueWithinBrackets = true, ListValueWithinBrackets = true)]
        [NPCSpawn(new[] { NPCSpawnContainerType.Add, NPCSpawnContainerType.Subtract, NPCSpawnContainerType.Override })]
        public NPCSpawnList<NPCSpawnEntry> NPCSpawnEntries
        {
            get { return (NPCSpawnList<NPCSpawnEntry>)GetValue(NPCSpawnEntriesProperty); }
            set { SetValue(NPCSpawnEntriesProperty, value); }
        }

        public static readonly DependencyProperty NPCSpawnLimitsProperty = DependencyProperty.Register(nameof(NPCSpawnLimits), typeof(NPCSpawnList<NPCSpawnLimit>), typeof(NPCSpawnContainer), new PropertyMetadata(null));
        [DataMember]
        [AggregateIniValueEntry(ValueWithinBrackets = true, ListValueWithinBrackets = true)]
        [NPCSpawn(new[] { NPCSpawnContainerType.Add, NPCSpawnContainerType.Subtract, NPCSpawnContainerType.Override })]
        public NPCSpawnList<NPCSpawnLimit> NPCSpawnLimits
        {
            get { return (NPCSpawnList<NPCSpawnLimit>)GetValue(NPCSpawnLimitsProperty); }
            set { SetValue(NPCSpawnLimitsProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override void InitializeFromINIValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var kvPair = value.Split(new[] { '=' }, 2);
            var kvValue = kvPair[1].Trim(' ');
            if (kvValue.StartsWith("("))
                kvValue = kvValue.Substring(1);
            if (kvValue.EndsWith(")"))
                kvValue = kvValue.Substring(0, kvValue.Length - 1);

            base.FromComplexINIValue(kvValue);
        }

        public override string ToINIValue()
        {
            throw new NotImplementedException();
        }

        public string ToIniValue(NPCSpawnContainerType containerType)
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            var result = new StringBuilder();
            result.Append("(");

            var delimiter = "";
            foreach (var prop in this.Properties)
            {
                var attrSpawn = prop.GetCustomAttributes(typeof(NPCSpawnAttribute), false).OfType<NPCSpawnAttribute>().FirstOrDefault();
                if (!attrSpawn?.ContainerTypes?.Contains(containerType) ?? false)
                    continue;

                result.Append(delimiter);

                var attr = prop.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

                result.Append($"{propName}=");
                if (attr?.ValueWithinBrackets ?? false)
                    result.Append("(");

                var val = prop.GetValue(this);
                var spawnCollection = val as ISpawnIniValuesCollection;
                if (spawnCollection != null)
                {
                    var iniVals = spawnCollection.ToIniValues(containerType);
                    var delimiter2 = "";
                    foreach (var iniVal in iniVals)
                    {
                        result.Append(delimiter2);
                        if (attr?.ListValueWithinBrackets ?? false)
                            result.Append($"({iniVal})");
                        else
                            result.Append(iniVal);

                        delimiter2 = DELIMITER.ToString();
                    }
                }
                else
                {
                    var collection = val as IIniValuesCollection;
                    if (collection != null)
                    {
                        var iniVals = collection.ToIniValues();
                        var delimiter2 = "";
                        foreach (var iniVal in iniVals)
                        {
                            result.Append(delimiter2);
                            if (attr?.ListValueWithinBrackets ?? false)
                                result.Append($"({iniVal})");
                            else
                                result.Append(iniVal);

                            delimiter2 = DELIMITER.ToString();
                        }
                    }
                    else
                    {
                        var propValue = StringUtils.GetPropertyValue(val, prop);
                        result.Append(propValue);
                    }
                }

                if (attr?.ValueWithinBrackets ?? false)
                    result.Append(")");

                delimiter = DELIMITER.ToString();
            }

            result.Append(")");
            return result.ToString();
        }

        public override string ToString()
        {
            return $"{NPCSpawnEntriesContainerClassString}; NPCSpawnEntries={NPCSpawnEntries.Count}; NPCSpawnLimits={NPCSpawnLimits.Count}";
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(NPCSpawnEntriesContainerClassString) && NPCSpawnEntries.Count == NPCSpawnLimits.Count;
    }

    [DataContract]
    public class NPCSpawnList<T> : AggregateIniValueList<T>, ISpawnIniValuesCollection
         where T : AggregateIniValue, new()
    {
        public NPCSpawnList(string aggregateValueName)
            : base(aggregateValueName, null)
        {
        }

        public override IEnumerable<string> ToIniValues()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> ToIniValues(NPCSpawnContainerType containerType)
        {
            if (string.IsNullOrWhiteSpace(IniCollectionKey))
                return this.Where(d => d.ShouldSave()).Cast<ISpawnIniValue>().Select(d => d.ToIniValue(containerType));

            return this.Where(d => d.ShouldSave()).Cast<ISpawnIniValue>().Select(d => $"{this.IniCollectionKey}={d.ToIniValue(containerType)}");
        }
    }

    [DataContract]
    public class NPCSpawnEntry : AggregateIniValue, ISpawnIniValue
    {
        public static readonly DependencyProperty AnEntryNameProperty = DependencyProperty.Register(nameof(AnEntryName), typeof(string), typeof(NPCSpawnEntry), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry]
        [NPCSpawn(new[] { NPCSpawnContainerType.Add, NPCSpawnContainerType.Override })]
        public string AnEntryName
        {
            get { return (string)GetValue(AnEntryNameProperty); }
            set { SetValue(AnEntryNameProperty, value); }
        }

        public static readonly DependencyProperty EntryWeightProperty = DependencyProperty.Register(nameof(EntryWeight), typeof(float), typeof(NPCSpawnEntry), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        [NPCSpawn(new[] { NPCSpawnContainerType.Add, NPCSpawnContainerType.Override })]
        public float EntryWeight
        {
            get { return (float)GetValue(EntryWeightProperty); }
            set { SetValue(EntryWeightProperty, value); }
        }

        public static readonly DependencyProperty NPCsToSpawnStringsProperty = DependencyProperty.Register(nameof(NPCsToSpawnStrings), typeof(string), typeof(NPCSpawnEntry), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry(ValueWithinBrackets = true)]
        [NPCSpawn(new[] { NPCSpawnContainerType.Add, NPCSpawnContainerType.Subtract, NPCSpawnContainerType.Override })]
        public string NPCsToSpawnStrings
        {
            get { return (string)GetValue(NPCsToSpawnStringsProperty); }
            set { SetValue(NPCsToSpawnStringsProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override void InitializeFromINIValue(string value)
        {
            base.FromComplexINIValue(value);
        }

        public override string ToINIValue()
        {
            throw new NotImplementedException();
        }

        public string ToIniValue(NPCSpawnContainerType containerType)
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            var result = new StringBuilder();

            var delimiter = "";
            foreach (var prop in this.Properties)
            {
                var attrSpawn = prop.GetCustomAttributes(typeof(NPCSpawnAttribute), false).OfType<NPCSpawnAttribute>().FirstOrDefault();
                if (!attrSpawn?.ContainerTypes?.Contains(containerType) ?? false)
                    continue;

                result.Append(delimiter);

                var attr = prop.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

                result.Append($"{propName}=");
                if (attr?.ValueWithinBrackets ?? false)
                    result.Append("(");

                var val = prop.GetValue(this);
                var spawnCollection = val as ISpawnIniValuesCollection;
                if (spawnCollection != null)
                {
                    var iniVals = spawnCollection.ToIniValues(containerType);
                    var delimiter2 = "";
                    foreach (var iniVal in iniVals)
                    {
                        result.Append(delimiter2);
                        if (attr?.ListValueWithinBrackets ?? false)
                            result.Append($"({iniVal})");
                        else
                            result.Append(iniVal);

                        delimiter2 = DELIMITER.ToString();
                    }
                }
                else
                {
                    var collection = val as IIniValuesCollection;
                    if (collection != null)
                    {
                        var iniVals = collection.ToIniValues();
                        var delimiter2 = "";
                        foreach (var iniVal in iniVals)
                        {
                            result.Append(delimiter2);
                            if (attr?.ListValueWithinBrackets ?? false)
                                result.Append($"({iniVal})");
                            else
                                result.Append(iniVal);

                            delimiter2 = DELIMITER.ToString();
                        }
                    }
                    else
                    {
                        var propValue = StringUtils.GetPropertyValue(val, prop);
                        result.Append(propValue);
                    }
                }

                if (attr?.ValueWithinBrackets ?? false)
                    result.Append(")");

                delimiter = DELIMITER.ToString();
            }

            return result.ToString();
        }

        public override string ToString()
        {
            return $"AnEntryName={AnEntryName}; EntryWeight={EntryWeight}; NPCsToSpawnStrings={NPCsToSpawnStrings}";
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(NPCsToSpawnStrings);
    }

    [DataContract]
    public class NPCSpawnLimit : AggregateIniValue, ISpawnIniValue
    {
        public static readonly DependencyProperty NPCClassStringProperty = DependencyProperty.Register(nameof(NPCClassString), typeof(string), typeof(NPCSpawnLimit), new PropertyMetadata(string.Empty));
        [DataMember]
        [AggregateIniValueEntry]
        [NPCSpawn(new[] { NPCSpawnContainerType.Add, NPCSpawnContainerType.Subtract, NPCSpawnContainerType.Override })]
        public string NPCClassString
        {
            get { return (string)GetValue(NPCClassStringProperty); }
            set { SetValue(NPCClassStringProperty, value); }
        }

        public static readonly DependencyProperty MaxPercentageOfDesiredNumToAllowProperty = DependencyProperty.Register(nameof(MaxPercentageOfDesiredNumToAllow), typeof(float), typeof(NPCSpawnLimit), new PropertyMetadata(1.0f));
        [DataMember]
        [AggregateIniValueEntry]
        [NPCSpawn(new[] { NPCSpawnContainerType.Add, NPCSpawnContainerType.Override })]
        public float MaxPercentageOfDesiredNumToAllow
        {
            get { return (float)GetValue(MaxPercentageOfDesiredNumToAllowProperty); }
            set { SetValue(MaxPercentageOfDesiredNumToAllowProperty, value); }
        }

        public override string GetSortKey()
        {
            return null;
        }

        public override bool IsEquivalent(AggregateIniValue other)
        {
            return false;
        }

        public override void InitializeFromINIValue(string value)
        {
            base.FromComplexINIValue(value);
        }

        public override string ToINIValue()
        {
            throw new NotImplementedException();
        }

        public string ToIniValue(NPCSpawnContainerType containerType)
        {
            GetPropertyInfos();
            if (this.Properties.Count == 0)
                return string.Empty;

            var result = new StringBuilder();

            var delimiter = "";
            foreach (var prop in this.Properties)
            {
                var attrSpawn = prop.GetCustomAttributes(typeof(NPCSpawnAttribute), false).OfType<NPCSpawnAttribute>().FirstOrDefault();
                if (!attrSpawn?.ContainerTypes?.Contains(containerType) ?? false)
                    continue;

                result.Append(delimiter);

                var attr = prop.GetCustomAttributes(typeof(AggregateIniValueEntryAttribute), false).OfType<AggregateIniValueEntryAttribute>().FirstOrDefault();
                var propName = string.IsNullOrWhiteSpace(attr?.Key) ? prop.Name : attr.Key;

                result.Append($"{propName}=");
                if (attr?.ValueWithinBrackets ?? false)
                    result.Append("(");

                var val = prop.GetValue(this);
                var spawnCollection = val as ISpawnIniValuesCollection;
                if (spawnCollection != null)
                {
                    var iniVals = spawnCollection.ToIniValues(containerType);
                    var delimiter2 = "";
                    foreach (var iniVal in iniVals)
                    {
                        result.Append(delimiter2);
                        if (attr?.ListValueWithinBrackets ?? false)
                            result.Append($"({iniVal})");
                        else
                            result.Append(iniVal);

                        delimiter2 = DELIMITER.ToString();
                    }
                }
                else
                {
                    var collection = val as IIniValuesCollection;
                    if (collection != null)
                    {
                        var iniVals = collection.ToIniValues();
                        var delimiter2 = "";
                        foreach (var iniVal in iniVals)
                        {
                            result.Append(delimiter2);
                            if (attr?.ListValueWithinBrackets ?? false)
                                result.Append($"({iniVal})");
                            else
                                result.Append(iniVal);

                            delimiter2 = DELIMITER.ToString();
                        }
                    }
                    else
                    {
                        var propValue = StringUtils.GetPropertyValue(val, prop);
                        result.Append(propValue);
                    }
                }


                if (attr?.ValueWithinBrackets ?? false)
                    result.Append(")");

                delimiter = DELIMITER.ToString();
            }

            return result.ToString();
        }

        public override string ToString()
        {
            return $"NPCClassString={NPCClassString}; MaxPercentageOfDesiredNumToAllow={MaxPercentageOfDesiredNumToAllow}";
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(NPCClassString);
    }
}
