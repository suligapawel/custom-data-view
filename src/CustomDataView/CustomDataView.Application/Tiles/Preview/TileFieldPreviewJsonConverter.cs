using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed class TileFieldPreviewJsonConverter : JsonConverter<TileFieldPreview>
{
    public override TileFieldPreview Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, TileFieldPreview value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(Name(options, nameof(TileFieldPreview.Label)), value.Label);
        if (value.Error is null)
        {
            writer.WritePropertyName(Name(options, nameof(TileFieldPreview.Value)));
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            writer.WritePropertyName(Name(options, nameof(TileFieldPreview.Error)));
            JsonSerializer.Serialize(writer, value.Error, options);
        }

        if (value.Hidden)
        {
            writer.WriteBoolean(Name(options, nameof(TileFieldPreview.Hidden)), true);
        }

        writer.WriteEndObject();
    }

    private static string Name(JsonSerializerOptions options, string name) => options.PropertyNamingPolicy?.ConvertName(name) ?? name;
}
