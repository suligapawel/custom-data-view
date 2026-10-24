using CustomDataView.Application.Exceptions;
using CustomDataView.Application.Tiles.Inputs;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.Services;
using CustomDataView.Domain.Services.Transformations;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Validation;

public sealed class TileReferencesValidator(
    IConnectionRepository connections,
    ITileRepository tiles,
    IEnumerable<ITransformation> transformations,
    CombinationService combinations)
{
    private readonly HashSet<string> catalog =
        transformations.Select(transformation => transformation.Key).ToHashSet(StringComparer.Ordinal);

    public void EnsureValid(TileDefinition definition, Guid? tileId = null)
    {
        var errors = Validate(definition, tileId);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    public IDictionary<string, string[]> Validate(TileInput input) => Validate(input.ToDefinition());

    public IDictionary<string, string[]> Validate(TileDefinition definition, Guid? tileId = null)
    {
        var connectionCache = new Dictionary<Guid, ConnectionDefinition>();
        var tileCache = new Dictionary<Guid, TileDefinition>();
        ConnectionDefinition ConnectionOf(Guid id)
            => connectionCache.TryGetValue(id, out var cached) ? cached : connectionCache[id] = connections.Get(id)?.Definition;
        TileDefinition TileOf(Guid id) => tileCache.TryGetValue(id, out var cached) ? cached : tileCache[id] = tiles.Get(id)?.Definition;

        var policy = new TileReferencesPolicy(new TileTree(TileOf), ConnectionOf, catalog, combinations);
        return new Dictionary<string, string[]>(policy.Check(definition, tileId));
    }
}
