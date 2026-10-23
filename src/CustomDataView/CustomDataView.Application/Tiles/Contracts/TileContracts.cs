using CustomDataView.Application.Tiles.Validation;
using CustomDataView.Domain.Dictionaries;
using CustomDataView.Domain.Repositories;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Contracts;

public sealed class TileContracts(IConnectionRepository connections)
{
    public TileContract Of(TileDefinition definition)
    {
        var parameters = new List<TileContractParameter>();
        foreach (var source in definition.Sources.Where(source => source.Kind == TileSourceKinds.Connection))
        {
            if (connections.Get(source.ConnectionId.Value) is not { } connection)
            {
                continue;
            }

            var bound = BoundInputs(definition, source.Key);
            parameters.AddRange(TileConnectionParameters.Of(connection.Definition)
                .Where(parameter => !bound.Contains(parameter.Name))
                .Select(parameter => new TileContractParameter(parameter.Name, parameter.Required && parameter.DefaultValue is null)));
        }

        return new TileContract(
            parameters
                .GroupBy(parameter => parameter.Name, StringComparer.Ordinal)
                .Select(group => new TileContractParameter(group.Key, group.Any(parameter => parameter.Required)))
                .ToList(),
            definition.Sources
                .Where(source => source.Kind == TileSourceKinds.Input && !BoundInputs(definition, source.Key).Contains(source.Key))
                .Select(source => source.Key)
                .ToList(),
            new TileContractOutputs(definition.Fields.Select(field => field.Label).ToList(), definition.Items is not null));
    }

    private static HashSet<string> BoundInputs(TileDefinition definition, string node)
        => definition.Bindings
            .Where(binding => binding.To == node)
            .SelectMany(binding => binding.Map)
            .Select(mapping => mapping.Parameter ?? mapping.Input)
            .ToHashSet(StringComparer.Ordinal);
}
