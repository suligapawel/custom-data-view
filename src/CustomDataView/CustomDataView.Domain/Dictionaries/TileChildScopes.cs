namespace CustomDataView.Domain.Dictionaries;

public static class TileChildScopes
{
    public const string Tile = "tile";
    public const string Item = "item";

    public static readonly IReadOnlyList<string> All = [Tile, Item];
}
