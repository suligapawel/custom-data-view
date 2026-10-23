namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class ViewTileRow
{
    public Guid ViewId { get; set; }

    public Guid TileId { get; set; }

    public int Position { get; set; }

    public string CssClass { get; set; }
}
