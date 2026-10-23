using System.Text.Json.Nodes;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public sealed class CoalesceCombination : Combination
{
    public override string Key => "coalesce";

    public override string Name => "Pierwsza wartość";

    public override string Description => "Zwraca pierwszą wartość pól różną od braku wartości.";

    public override int MinFields => 2;

    public override JsonNode Combine(string parameter, IReadOnlyList<CombinationInput> inputs)
        => inputs.FirstOrDefault(input => input.Value is not null)?.Value.DeepClone();
}
