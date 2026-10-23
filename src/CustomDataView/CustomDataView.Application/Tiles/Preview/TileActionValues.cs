using System.Text.Json.Nodes;
using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Preview;

public static class TileActionValues
{
    public static TileActionPreview Preview(TileAction action, Func<string, TileFieldPreview> field)
    {
        var parameters = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        var inputs = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        foreach (var mapping in action.Map)
        {
            var value = field(mapping.Field);
            if (value.Value is null)
            {
                continue;
            }

            (mapping.Parameter is null ? inputs : parameters)[mapping.Parameter ?? mapping.Input] = value.Value.DeepClone();
        }

        return new TileActionPreview(
            action.Key,
            action.Label,
            action.Type,
            action.TileId,
            new TileValues { Parameters = parameters, Inputs = inputs }.Compact());
    }
}
