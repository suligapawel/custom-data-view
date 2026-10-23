using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.WebApi.Requests;

public sealed record TileRequest(string Name, IReadOnlyList<TileFieldRequest> Fields)
{
    public IDictionary<string, string[]> Validate() => TileDefinitionValidator.Validate(ToInput());

    public TileDefinition ToDefinition() => ToInput().ToDefinition();

    public TileInput ToInput()
        => new TileInput(
            Name,
            (Fields ?? [])
                .Select(field => new TileFieldInput(
                    field.ConnectionId,
                    field.Path,
                    field.Label,
                    (field.Transformations ?? []).Select(ToInput).ToList(),
                    field.Hidden ?? false))
                .ToList());

    private static TransformationStepInput ToInput(TransformationStepRequest step)
        => new TransformationStepInput(
            step.Key,
            (step.Parameters ?? new Dictionary<string, JsonElement>())
                .ToDictionary(parameter => parameter.Key, parameter => JsonNode.Parse(parameter.Value.GetRawText())));
}

public sealed record TileFieldRequest(
    Guid? ConnectionId,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepRequest> Transformations,
    bool? Hidden);

public sealed record TransformationStepRequest(string Key, Dictionary<string, JsonElement> Parameters);
