using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileInput(
    string Name,
    string Display,
    IReadOnlyList<TileSourceInput> Sources,
    TileItemsInput Items,
    IReadOnlyList<TileFieldInput> Fields)
{
    public IReadOnlyList<TileChildInput> Children { get; init; } = [];

    public IReadOnlyList<TileBindingInput> Bindings { get; init; } = [];

    public TilePaginationInput Pagination { get; init; }

    public IReadOnlyList<TileActionInput> Actions { get; init; } = [];

    public const string DefaultPageNumberParameter = "pageNumber";
    public const string DefaultPageSizeParameter = "pageSize";

    public bool IsForm => Display == TileDisplays.Form;

    public bool IsTabs => Display == TileDisplays.Tabs;

    public TileDefinition ToDefinition()
        => IsForm
            ? ToDataDefinition() with
            {
                Fields = [],
                FormFields = Fields.Select(field => new TileFormField(field.Label, field.Type, field.Required, field.Default)).ToList()
            }
            : ToDataDefinition();

    private TileDefinition ToDataDefinition()
        => new TileDefinition(
            Name,
            Display,
            Sources.Select(source => new TileSource(source.Key, source.Kind, source.ConnectionId)).ToList(),
            Items is null ? null : new TileItems(Items.Source, Items.Path, Items.Key),
            Fields
                .Select(field => new TileField(
                    field.Source,
                    field.Path,
                    field.Label,
                    field.Transformations
                        .Select(step => new TransformationStep(step.Key, TransformationParameterValues.ToText(step.Parameter)))
                        .ToList(),
                    field.Hidden,
                    string.IsNullOrWhiteSpace(field.Role) ? null : field.Role)
                {
                    Combination = field.Combine is { } combine
                        ? new TileFieldCombination(combine.Key, TransformationParameterValues.ToText(combine.Parameter), combine.Fields ?? [])
                        : null
                })
                .ToList())
        {
            Pagination = Pagination is null
                ? null
                : new TilePagination(
                    string.IsNullOrWhiteSpace(Pagination.PageNumberParameter) ? DefaultPageNumberParameter : Pagination.PageNumberParameter,
                    string.IsNullOrWhiteSpace(Pagination.PageSizeParameter) ? DefaultPageSizeParameter : Pagination.PageSizeParameter,
                    Pagination.PageSize ?? 0,
                    string.IsNullOrWhiteSpace(Pagination.TotalPath) ? null : Pagination.TotalPath),
            Actions = Actions
                .Select(action => new TileAction(
                    action.Key,
                    action.Label?.Trim(),
                    action.Type ?? TileActionTypes.Button,
                    action.TileId ?? Guid.Empty,
                    action.Scope ?? (Items is null ? TileActionScopes.Tile : TileActionScopes.Item),
                    action.Map.Select(mapping => new TileActionMapping(mapping.Field, mapping.Parameter, mapping.Input)).ToList()))
                .ToList(),
            Children = Children
                .Select(child => new TileChild(
                    child.Key,
                    child.TileId ?? Guid.Empty,
                    child.Position ?? 0,
                    string.IsNullOrWhiteSpace(child.CssClass) ? null : child.CssClass.Trim(),
                    child.Scope ?? TileChildScopes.Tile,
                    child.Load ?? TileChildLoads.Eager,
                    string.IsNullOrWhiteSpace(child.Label) ? null : child.Label.Trim()))
                .ToList(),
            Bindings = Bindings
                .Select(binding => new TileBinding(
                    binding.From,
                    binding.To,
                    binding.Map
                        .Select(mapping => new TileBindingMapping(mapping.Field, mapping.Parameter, mapping.Input, mapping.Scope))
                        .ToList()))
                .ToList()
        };
}
