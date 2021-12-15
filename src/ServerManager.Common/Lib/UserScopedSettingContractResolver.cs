using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;

namespace ServerManagerTool.Common.Lib
{
    public class UserScopedSettingContractResolver : DefaultContractResolver
    {
        public static readonly UserScopedSettingContractResolver Instance = new UserScopedSettingContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            var customAttributes = member.CustomAttributes ?? new CustomAttributeData[0];
            if (customAttributes.Any(a => a.AttributeType == typeof(System.Configuration.UserScopedSettingAttribute)))
            {
                property.ShouldSerialize = instance => { return property.PropertyType.IsValueType || property.PropertyType == typeof(string); };
            }
            else
            {
                property.ShouldSerialize = instance => { return false; };
            }

            return property;
        }
    }
}
