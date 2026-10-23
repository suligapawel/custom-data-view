using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.WebApi.Requests;

public sealed record TileRequest(string Name, IReadOnlyList<TileSourceRequest> Sources, IReadOnlyList<TileFieldRequest> Fields)
{
    public IDictionary<string, string[]> Validate() => TileDefinitionValidator.Validate(ToInput());

    public TileDefinition ToDefinition() => ToInput().ToDefinition();

    public TileInput ToInput()
        => new TileInput(
            Name,
            (Sources ?? []).Select(source => new TileSourceInput(source.Key, source.Kind, source.ConnectionId)).ToList(),
            (Fields ?? [])
                .Select(field => new TileFieldInput(
                    field.Source,
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

public sealed record TileSourceRequest(string Key, string Kind, Guid? ConnectionId);

public sealed record TileFieldRequest(
    string Source,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepRequest> Transformations,
    bool? Hidden);

public sealed record TransformationStepRequest(string Key, Dictionary<string, JsonElement> Parameters);
