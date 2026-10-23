namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileBindingRow
{
    public Guid TileId { get; set; }

    public string TargetNode { get; set; }

    public string TargetKind { get; set; }

    public string TargetName { get; set; }

    public string SourceNode { get; set; }

    public string SourceField { get; set; }

    public string SourceScope { get; set; }

    public int Position { get; set; }
}
