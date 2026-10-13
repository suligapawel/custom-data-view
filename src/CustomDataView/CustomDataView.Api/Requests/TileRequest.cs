using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Core.Tiles.Definitions;
using CustomDataView.Core.Transformations.Execution;

namespace CustomDataView.Api.Requests;

public sealed record TileRequest(string Name, IReadOnlyList<TileFieldRequest> Fields)
{
    public TileDefinition ToDefinition()
        => new TileDefinition(
            Name,
            Fields
                .Select(field => new TileField(
                    field.ConnectionId,
                    field.Path,
                    field.Label,
                    (field.Transformations ?? []).Select(ToStep).ToList()))
                .ToList());

    private static TransformationStep ToStep(TransformationStepRequest step)
        => new TransformationStep(
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
