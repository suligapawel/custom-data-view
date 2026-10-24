using System.Text.Json;
using System.Text.Json.Nodes;

namespace CustomDataView.Domain.ValueObjects;

public sealed record ParameterDeclaration(string Name, bool Required, string DefaultValue)
{
    public bool IsRequiredWithDefault => Required && DefaultValue is not null;

    public string DefaultText
        => DefaultValue is not null && JsonNode.Parse(DefaultValue) is JsonValue value && value.GetValueKind() == JsonValueKind.String
            ? value.GetValue<string>()
            : null;
}
