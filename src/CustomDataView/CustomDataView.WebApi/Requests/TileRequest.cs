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
    IReadOnlyList<TileChildRequest> Children,
    IReadOnlyList<TileBindingRequest> Bindings,
    TilePaginationRequest Pagination,
    IReadOnlyList<TileActionRequest> Actions)
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
                    field.Role)
                {
                    Type = field.Type,
                    Required = field.Required ?? false,
                    Default = field.Default is { } value ? JsonNode.Parse(value.GetRawText()) : null,
                    Combine = field.Combine is { } combine
                        ? new TileFieldCombinationInput(
                            combine.Key,
                            combine.Parameter is { } parameter ? JsonNode.Parse(parameter.GetRawText()) : null,
                            combine.Fields ?? [])
                        : null
                })
                .ToList())
        {
            Pagination = Pagination is null
                ? null
                : new TilePaginationInput(Pagination.PageNumberParameter, Pagination.PageSizeParameter, Pagination.PageSize, Pagination.TotalPath),
            Actions = (Actions ?? [])
                .Select(action => new TileActionInput(
                    action.Key,
                    action.Label,
                    action.Type,
                    action.TileId,
                    action.Scope,
                    (action.Map ?? []).Select(mapping => new TileActionMappingInput(mapping.Field, mapping.Parameter, mapping.Input)).ToList()))
                .ToList(),
            Children = (Children ?? [])
                .Select(child => new TileChildInput(child.Key, child.TileId, child.Position, child.CssClass, child.Scope, child.Load, child.Label))
                .ToList(),
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
            step.Parameter is { } parameter ? JsonNode.Parse(parameter.GetRawText()) : null);
}

public sealed record TileActionRequest(
    string Key,
    string Label,
    string Type,
    Guid? TileId,
    string Scope,
    IReadOnlyList<TileActionMappingRequest> Map);

public sealed record TileActionMappingRequest(string Field, string Parameter, string Input);

public sealed record TilePaginationRequest(string PageNumberParameter, string PageSizeParameter, int? PageSize, string TotalPath);

public sealed record TileSourceRequest(string Key, string Kind, Guid? ConnectionId);

public sealed record TileItemsRequest(string Source, string Path, string Key);

public sealed record TileChildRequest(string Key, Guid? TileId, int? Position, string CssClass, string Scope, string Load, string Label);

public sealed record TileBindingRequest(string From, string To, IReadOnlyList<TileBindingMappingRequest> Map);

public sealed record TileBindingMappingRequest(string Field, string Parameter, string Input, string Scope);

public sealed record TileFieldRequest(
    string Source,
    string Path,
    string Label,
    IReadOnlyList<TransformationStepRequest> Transformations,
    bool? Hidden,
    string Role,
    string Type,
    bool? Required,
    JsonElement? Default,
    TileFieldCombinationRequest Combine);

public sealed record TileFieldCombinationRequest(string Key, JsonElement? Parameter, IReadOnlyList<string> Fields);

public sealed record TransformationStepRequest(string Key, JsonElement? Parameter);
