namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileSourceRow
{
    public Guid TileId { get; set; }

    public string Key { get; set; }

    public int Position { get; set; }

    public string Kind { get; set; }

    public Guid? ConnectionId { get; set; }
}
