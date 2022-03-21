using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace OrganisationRegistryTools.Import;

public class TrimStringConverter : JsonConverter
{
    private static readonly Regex SpaceRemover = new(@"\s+", RegexOptions.Compiled);

    public override bool CanRead => true;
    public override bool CanWrite => false;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        return TrimInputField((string?)reader.Value);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException(
            "Unnecessary because CanWrite is false. The type will skip the converter.");
    }

    private static string? TrimInputField(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        input = input.Trim();
        input = SpaceRemover.Replace(input, " ");

        return input;
    }
}