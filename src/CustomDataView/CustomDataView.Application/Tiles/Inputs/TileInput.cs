using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Inputs;

public sealed record TileInput(
    string Name,
    string Display,
    IReadOnlyList<TileSourceInput> Sources,
    TileItemsInput Items,
    IReadOnlyList<TileFieldInput> Fields)
{
    public IReadOnlyList<TileBindingInput> Bindings { get; init; } = [];

    public bool IsForm => Display == TileDisplays.Form;

    public TileDefinition ToDefinition()
        => IsForm ? ToFormDefinition() : ToDataDefinition();

    private TileDefinition ToFormDefinition()
        => new TileDefinition(Name, Display, [], null, [])
        {
            FormFields = Fields.Select(field => new TileFormField(field.Label, field.Type, field.Required, field.Default)).ToList()
        };

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
                        .Select(step => new TransformationStep(step.Key, step.Parameters))
                        .ToList(),
                    field.Hidden,
                    string.IsNullOrWhiteSpace(field.Role) ? null : field.Role))
                .ToList())
        {
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
