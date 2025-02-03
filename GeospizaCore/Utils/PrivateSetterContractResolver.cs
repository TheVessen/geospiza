using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GeospizaCore.Utils;

/// <summary>
///     Custom contract resolver to handle private setters during JSON deserialization.
/// </summary>
public class PrivateSetterContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        if (!property.Writable)
        {
            var prop = member as PropertyInfo;
            var hasPrivateSetter = prop?.GetSetMethod(true) != null;
            property.Writable = hasPrivateSetter;
        }

        return property;
    }
}