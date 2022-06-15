using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OrganisationRegistryTools.Import;

public class OrganisationRegistryContractResolver : DefaultContractResolver
{
    public bool SetStringDefaultValueToEmptyString { get; set; }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);

        if (prop.PropertyType == typeof(string) && SetStringDefaultValueToEmptyString)
            prop.DefaultValue = "";

        return prop;
    }
}
