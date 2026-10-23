using System.Text.Json.Nodes;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public sealed class ObjectCombination : Combination
{
    public override string Key => "object";

    public override string Name => "Obiekt";

    public override string Description => "Grupuje pola w obiekt, którego kluczami są etykiety pól.";

    public override JsonNode Combine(string parameter, IReadOnlyList<CombinationInput> inputs)
        => throw new NotSupportedException();
}
