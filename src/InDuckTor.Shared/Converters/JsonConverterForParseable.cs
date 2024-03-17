using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InDuckTor.Shared.Converters;

 
public class JsonConverterForParseable<TParseable> : JsonConverter<TParseable> where TParseable : IParsable<TParseable>
{
    public override TParseable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = JsonSerializer.Deserialize<string>(ref reader, options);
        return stringValue == null ? default : TParseable.Parse(stringValue, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, TParseable value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.ToString(), options);
}