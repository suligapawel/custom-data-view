namespace CustomDataView.Core.Tiles.Paths;

public sealed record JsonPath(IReadOnlyList<JsonPathSegment> Segments);
