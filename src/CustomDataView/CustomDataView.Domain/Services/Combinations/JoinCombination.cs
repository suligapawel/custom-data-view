using System.Text.Json.Nodes;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public sealed class JoinCombination : Combination
{
    private static readonly CombinationParameterDescriptor SeparatorParameter =
        new("separator", TransformationParameterTypes.String, false);

    public override string Key => "join";

    public override string Name => "Złączenie";

    public override string Description => "Łączy wartości pól w kolejności listy separatorem; pomija brak wartości i rozkłada listy.";

    public override CombinationParameterDescriptor Parameter => SeparatorParameter;

    public override JsonNode Combine(string parameter, IReadOnlyList<CombinationInput> inputs)
    {
        var values = inputs
            .SelectMany(input => input.Value is JsonArray array ? array : [input.Value])
            .Where(value => value is not null)
            .Select(value => CombinationValue.ToText(Key, value))
            .ToList();
        return values.Count == 0 ? null : JsonValue.Create(string.Join(parameter ?? string.Empty, values));
    }
}
