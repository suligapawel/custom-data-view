using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Preview;

public static class TileFormValues
{
    public static IReadOnlyList<TileFieldPreview> Preview(
        IReadOnlyList<TileFormField> fields,
        IReadOnlyDictionary<string, JsonElement> values)
        => fields.Select(field => Preview(field, values)).ToList();

    private static TileFieldPreview Preview(TileFormField field, IReadOnlyDictionary<string, JsonElement> values)
    {
        var provided = values.FirstOrDefault(value => TileLabels.AreEqual(value.Key, field.Label));
        var descriptor = new TileFormFieldPreview(field.Type, field.Required, field.Default?.DeepClone());
        if (provided.Key is null || provided.Value.ValueKind == JsonValueKind.Null)
        {
            return new TileFieldPreview(field.Label, field.Default?.DeepClone()) { Form = descriptor };
        }

        var value = JsonNode.Parse(provided.Value.GetRawText());
        return TileFormFieldTypes.Matches(field.Type, value)
            ? new TileFieldPreview(field.Label, value) { Form = descriptor }
            : new TileFieldPreview(field.Label, value, new TileFieldError(TileFieldErrorCodes.InvalidValue)) { Form = descriptor };
    }
}
