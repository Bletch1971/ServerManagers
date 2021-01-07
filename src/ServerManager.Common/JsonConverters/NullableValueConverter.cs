using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServerManagerTool.Common.Interfaces;
using ServerManagerTool.Common.Model;
using System;

namespace ServerManagerTool.Common.JsonConverters
{
    public class NullableValueConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(NullableValue<int>))
            {
                return true;
            }
            else if (objectType == typeof(NullableValue<float>))
            {
                return true;
            }

            return false;
        }

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                // Create target object based on objectType
                var target = Activator.CreateInstance(objectType) as INullableValue;
                target?.SetValue(existingValue);

                return target;
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                // Load JObject from stream
                var jObject = JObject.Load(reader);

                // Create target object based on objectType
                var target = Activator.CreateInstance(objectType);

                // Populate the object properties
                serializer.Populate(jObject.CreateReader(), target);

                return target;
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                var jValue = JToken.Load(reader) as JValue;

                // Create target object based on objectType
                var target = Activator.CreateInstance(objectType) as NullableValue<int>;
                target?.SetValue(existingValue);

                if (target != null && jValue != null && jValue.Value != null && int.TryParse(jValue.Value.ToString(), out int result))
                {
                    // Populate the object properties
                    target.SetValue(result);

                    return target;
                }
            }

            if (reader.TokenType == JsonToken.Float)
            {
                var jValue = JToken.Load(reader) as JValue;

                // Create target object based on objectType
                var target = Activator.CreateInstance(objectType) as NullableValue<int>;
                target?.SetValue(existingValue);

                if (target != null && jValue != null && jValue.Value != null && float.TryParse(jValue.Value.ToString(), out float result))
                {
                    // Populate the object properties
                    target.SetValue(result);

                    return target;
                }
            }

            return Activator.CreateInstance(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null && value is NullableValue<int> && ((NullableValue<int>)value).HasValue)
            {
                serializer.Serialize(writer, ((NullableValue<int>)value).Value);
            }
            else if (value != null && value is NullableValue<float> && ((NullableValue<float>)value).HasValue)
            {
                serializer.Serialize(writer, ((NullableValue<float>)value).Value);
            }
            else
            {
                serializer.Serialize(writer, null);
            }
        }
    }
}
