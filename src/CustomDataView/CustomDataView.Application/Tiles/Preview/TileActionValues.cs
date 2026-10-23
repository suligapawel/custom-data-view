using System.Text.Json.Nodes;
using CustomDataView.Application.Tiles.Contracts;
using CustomDataView.Domain.Policies;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Preview;

public static class TileActionValues
{
    public static TileActionPreview Preview(
        TileAction action,
        TileDefinition definition,
        TileContract target,
        Func<string, TileFieldPreview> field)
    {
        var undeclared = action.Map
            .Where(mapping => mapping.Parameter is not null
                ? target.Parameters.All(parameter => parameter.Name != mapping.Parameter)
                : !target.Inputs.Contains(mapping.Input))
            .Select(mapping => mapping.Parameter ?? mapping.Input)
            .Select(name => new TileParameterError(name, $"Akcja wskazuje wejście '{name}', którego kafelek docelowy nie deklaruje.", true))
            .ToList();
        if (undeclared.Count > 0)
        {
            return Failed(action, new TileFieldError(TileFieldErrorCodes.InvalidBinding, Parameters: undeclared));
        }

        var parameters = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        var inputs = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        foreach (var mapping in action.Map)
        {
            var value = field(mapping.Field);
            if (value.Error is not null)
            {
                var source = definition.Fields.First(candidate => TileLabels.AreEqual(candidate.Label, mapping.Field)).Source;
                return Failed(action, new TileFieldError(
                    TileFieldErrorCodes.DependencyFailed,
                    Cause: new TileFieldErrorCause(source, value.Label, value.Error.Code)));
            }

            if (value.Value is not null)
            {
                (mapping.Parameter is null ? inputs : parameters)[mapping.Parameter ?? mapping.Input] = value.Value.DeepClone();
            }
        }

        return new TileActionPreview(
            action.Key,
            action.Label,
            action.Type,
            action.TileId,
            new TileValues { Parameters = parameters, Inputs = inputs }.Compact());
    }

    private static TileActionPreview Failed(TileAction action, TileFieldError error)
        => new(action.Key, action.Label, action.Type, action.TileId, null, error);
}
