namespace CustomDataView.Core.Views.Definitions;

public sealed record ViewInput(string Name, IReadOnlyList<ViewTileInput> Tiles)
{
    public ViewDefinition ToDefinition()
        => new ViewDefinition(
            Name,
            Tiles.Select(tile => new ViewTile(tile.TileId.Value, tile.Position.Value, CssClassOf(tile))).ToList());

    private static string CssClassOf(ViewTileInput tile)
        => string.IsNullOrWhiteSpace(tile.CssClass) ? null : tile.CssClass.Trim();
}
