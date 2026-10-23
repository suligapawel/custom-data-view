using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Exceptions;
using CustomDataView.Domain.Services.Transformations;

namespace CustomDataView.Domain.Services.Combinations;

public static class CombinationValue
{
    public static string ToText(string key, JsonNode value)
        => value?.GetValueKind() switch
        {
            null => string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number or JsonValueKind.String => TransformationValue.ToText(key, TransformationValue.FromJson(value)),
            _ => throw new CombinationFailedException(key, CombinationFailureReasons.UnsupportedValue)
        };
}
