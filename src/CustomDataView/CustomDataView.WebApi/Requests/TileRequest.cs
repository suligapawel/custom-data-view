using System.Text.Json;
using System.Text.Json.Nodes;
using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.WebApi.Requests;

public sealed record TileRequest(
    string Name,
    string Display,
    IReadOnlyList<TileSourceRequest> Sources,
    TileItemsRequest Items,
    IReadOnlyList<TileFieldRequest> Fields,
    IReadOnlyList<TileBindingRequest> Bindings)
{
    public IDictionary<string, string[]> Validate() => TileDefinitionValidator.Validate(ToInput());

    public TileDefinition ToDefinition() => ToInput().ToDefinition();

    public TileInput ToInput()
        => new TileInput(
            Name,
            Display,
            (Sources ?? []).Select(source => new TileSourceInput(source.Key, source.Kind, source.ConnectionId)).ToList(),
            Items is null ? null : new TileItemsInput(Items.Source, Items.Path, Items.Key),
            (Fields ?? [])
                .Select(field => new TileFieldInput(
                    field.Source,
                    field.Path,
                    field.Label,
                    (field.Transformations ?? []).Select(ToInput).ToList(),
                    field.Hidden ?? false,
                    field.Role))
                .ToList())
        {
            Bindings = (Bindings ?? [])
                .Select(binding => new TileBindingInput(
                    binding.From,
                    binding.To,
                    (binding.Map ?? [])
                        .Select(mapping => new TileBindingMappingInput(mapping.Field, mapping.Parameter, mapping.Input, mapping.Scope))
                        .ToList()))
                .ToList()
        };

    private static TransformationStepInput ToInput(TransformationStepRequest step)
        => new TransformationStepInput(
            step.Key,
            (step.Parameters ?? new Dictionary<string, JsonElement>())
                .ToDictionary(parameter => parameter.Key, parameter => JsonNode.Parse(parameter.Value.GetRawText())));
}

public sealed record TileSourceRequest(string Key, string Kind, Guid? ConnectionId);

public sealed record TileItemsRequest(string Source, string Path, string Key);

public sealed record TileBindingRequest(string From, string To, IReadOnlyList<TileBindingMappingRequest> Map);

public sealed record TileBindingMappingRequest(string Field, string Parameter, string Input, string Scope);

public sealed record TileFieldRequest(
    string Source,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepRequest> Transformations,
    bool? Hidden,
    string Role);

public sealed record TransformationStepRequest(string Key, Dictionary<string, JsonElement> Parameters);
