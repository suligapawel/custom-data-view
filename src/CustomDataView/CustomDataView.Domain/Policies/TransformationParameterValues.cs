using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Domain.Policies;

public static class TransformationParameterValues
{
    public static bool IsStorable(JsonNode value)
        => value is null || value.GetValueKind() is JsonValueKind.String or JsonValueKind.Number;

    public static string ToText(JsonNode value)
        => value?.GetValueKind() switch
        {
            null => null,
            JsonValueKind.String => value.GetValue<string>(),
            _ => value.ToJsonString()
        };
}
