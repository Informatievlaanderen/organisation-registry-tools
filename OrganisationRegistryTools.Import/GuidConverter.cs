using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OrganisationRegistryTools.Import;

public class GuidConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Guid);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        var value = reader.Value;
        var stringValue = value?.ToString();

        return token.Type switch
        {
            JTokenType.Null => Guid.Empty,
            JTokenType.String => string.IsNullOrWhiteSpace(stringValue) ? Guid.Empty : new Guid(stringValue),
            _ => throw new ArgumentException("Invalid token type")
        };
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (EqualityComparer<Guid?>.Default.Equals((Guid?)value, default))
            writer.WriteValue(string.Empty);
        else if (EqualityComparer<Guid>.Default.Equals((Guid)(value!), default))
            writer.WriteValue(string.Empty);
        else
            writer.WriteValue((Guid)value);
    }
}