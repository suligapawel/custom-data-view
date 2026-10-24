using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.Services.Transformations;

namespace CustomDataView.Domain.ValueObjects;

public sealed partial record TileDefinition(
    string Name,
    string Display,
    IReadOnlyList<TileSource> Sources,
    TileItems Items,
    IReadOnlyList<TileField> Fields)
{
    public IReadOnlyList<TileFormField> FormFields { get; init; } = [];

    public IReadOnlyList<TileChild> Children { get; init; } = [];

    public IReadOnlyList<TileBinding> Bindings { get; init; } = [];

    public TilePagination Pagination { get; init; }

    public IReadOnlyList<TileAction> Actions { get; init; } = [];

    public bool IsForm => Display == TileDisplays.Form;

    public bool IsTabs => Display == TileDisplays.Tabs;

    public IReadOnlyList<string> NodeKeys => [.. Sources.Select(source => source.Key), .. Children.Select(child => child.Key)];

    public IReadOnlyList<string> OutputLabels => [.. FormFields.Select(formField => formField.Label), .. Fields.Select(tileField => tileField.Label)];

    public bool HasOutput(string label) => OutputLabels.Any(candidate => TileLabels.AreEqual(candidate, label));

    public TileField Field(string label) => Fields.FirstOrDefault(field => TileLabels.AreEqual(field.Label, label));

    public TileSource Source(string key) => Sources.FirstOrDefault(source => source.Key == key);

    public TileChild Child(string key) => Children.FirstOrDefault(child => child.Key == key);

    public bool IsItemField(TileField field) => Items is not null && (field.Source == Items.Source || field.IsCombined);

    public IEnumerable<DictionaryReference> DictionaryReferences()
        => Fields
            .SelectMany(field => field.Transformations)
            .Where(step => step.Key == MapperTransformation.TransformationKey)
            .Select(step => DictionaryReference.TryParse(step.Parameter, out var reference) ? reference : null)
            .Where(reference => reference is not null);

    public TileDefinition AsDictionarySource()
        => this with
        {
            Sources = Sources.Where(source => source.Key == Items.Source).ToList(),
            Fields = Fields.Where(IsItemField).ToList(),
            Children = [],
            Bindings = [],
            Actions = [],
            Pagination = null
        };

    public IReadOnlySet<string> BoundInputs(string node)
        => Bindings
            .Where(binding => binding.To == node)
            .SelectMany(binding => binding.Map)
            .Select(mapping => mapping.Parameter ?? mapping.Input)
            .ToHashSet(StringComparer.Ordinal);

    public IReadOnlyList<TileField> InputsOf(TileField field, CombinationService combinations)
        => combinations.Find(field.Combination.Key) is { } combination
            ? combination.Inputs(field.Combination.Parameter, field.Combination.Fields)
                .Select(Field)
                .Where(input => input is not null)
                .ToList()
            : [];

    public IReadOnlyList<(string From, string To)> BindingEdges(CombinationService combinations)
        => Bindings
            .SelectMany(binding => binding.Map.SelectMany(mapping => EdgeSources(binding, mapping, combinations)
                .Select(from => (From: from, To: binding.To.Split(TileNodeKeys.PathSeparator)[0]))))
            .ToList();

    public TileContract Contract(Func<Guid, ConnectionDefinition> connectionOf)
    {
        var parameters = new List<TileContractParameter>();
        foreach (var source in Sources.Where(source => source.Kind == TileSourceKinds.Connection))
        {
            if (connectionOf(source.ConnectionId.Value) is not { } connection)
            {
                continue;
            }

            var bound = BoundInputs(source.Key);
            parameters.AddRange(connection.Parameters
                .Where(parameter => !bound.Contains(parameter.Name))
                .Select(parameter => new TileContractParameter(parameter.Name, parameter.Required && parameter.DefaultValue is null)));
        }

        return new TileContract(
            parameters
                .GroupBy(parameter => parameter.Name, StringComparer.Ordinal)
                .Select(group => new TileContractParameter(group.Key, group.Any(parameter => parameter.Required)))
                .ToList(),
            Sources
                .Where(source => source.Kind == TileSourceKinds.Input && !BoundInputs(source.Key).Contains(source.Key))
                .Select(source => source.Key)
                .ToList(),
            new TileContractOutputs(OutputLabels, Items is not null));
    }

    private IEnumerable<string> EdgeSources(TileBinding binding, TileBindingMapping mapping, CombinationService combinations)
    {
        if (binding.From != TileNodeKeys.Parent)
        {
            return [binding.From.Split(TileNodeKeys.PathSeparator)[0]];
        }

        var field = Field(mapping.Field);
        return field is { IsCombined: true }
            ? InputsOf(field, combinations).Select(input => input.Source)
            : [field?.Source ?? binding.From];
    }
}
