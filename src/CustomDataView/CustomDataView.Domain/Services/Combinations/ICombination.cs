using System.Text.Json.Nodes;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Domain.Services.Combinations;

public interface ICombination
{
    string Key { get; }

    string Name { get; }

    string Description { get; }

    CombinationParameterDescriptor Parameter { get; }

    int MinFields { get; }

    int? MaxFields { get; }

    bool AcceptsCollections { get; }

    bool AcceptsTransformations { get; }

    IReadOnlyList<string> Inputs(string parameter, IReadOnlyList<string> fields);

    IReadOnlyList<CombinationError> Validate(string parameter, IReadOnlyList<string> fields);

    JsonNode Combine(string parameter, IReadOnlyList<CombinationInput> inputs);
}
