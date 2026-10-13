using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Tiles.Validation;

namespace CustomDataView.Api.Requests;

public sealed record TileRequest(string Name, IReadOnlyList<TileFieldRequest> Fields)
{
    public IDictionary<string, string[]> Validate() => TileDefinitionValidator.Validate(ToInput());

    public TileDefinition ToDefinition() => ToInput().ToDefinition();

    private TileInput ToInput()
        => new TileInput(
            Name,
            Fields
                .Select(field => new TileFieldInput(
                    field.ConnectionId,
                    field.Path,
                    field.Label,
                    (field.Transformations ?? []).Select(ToInput).ToList()))
                .ToList());

    private static TransformationStepInput ToInput(TransformationStepRequest step)
        => new TransformationStepInput(
            step.Key,
            (step.Parameters ?? new Dictionary<string, JsonElement>())
                .ToDictionary(parameter => parameter.Key, parameter => JsonNode.Parse(parameter.Value.GetRawText())));
}

public sealed record TileFieldRequest(
    Guid ConnectionId,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepRequest> Transformations);

public sealed record TransformationStepRequest(string Key, Dictionary<string, JsonElement> Parameters);
