namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileChildRow
{
    public Guid ParentTileId { get; set; }

    public string Key { get; set; }

    public Guid ChildTileId { get; set; }

    public int Position { get; set; }

    public string CssClass { get; set; }

    public string Scope { get; set; }

    public string Load { get; set; }

    public string Label { get; set; }
}
