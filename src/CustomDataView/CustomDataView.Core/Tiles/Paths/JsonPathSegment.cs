namespace CustomDataView.Core.Tiles.Paths;

public abstract record JsonPathSegment
{
    public sealed record Property(string Name) : JsonPathSegment;

    public sealed record Index(int Value) : JsonPathSegment;

    public sealed record Wildcard : JsonPathSegment;
}
