namespace CustomDataView.Domain.Dictionaries;

public static class TileChildLoads
{
    public const string Eager = "eager";
    public const string Lazy = "lazy";

    public static readonly IReadOnlyList<string> All = [Eager, Lazy];
}
