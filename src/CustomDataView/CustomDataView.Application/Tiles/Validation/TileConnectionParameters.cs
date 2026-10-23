using CustomDataView.Domain.ValueObjects;

namespace CustomDataView.Application.Tiles.Validation;

public static class TileConnectionParameters
{
    public static IReadOnlyList<ParameterDeclaration> Of(ConnectionDefinition definition)
        => [.. definition.PathParameters, .. definition.QueryParameters, .. (definition as PostConnectionDefinition)?.BodyParameters ?? []];

    public static HashSet<string> Names(ConnectionDefinition definition)
        => Of(definition).Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
}
