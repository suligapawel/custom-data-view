namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileActionMappingRow
{
    public Guid TileId { get; set; }

    public string ActionKey { get; set; }

    public string TargetKind { get; set; }

    public string TargetName { get; set; }

    public string SourceField { get; set; }

    public int Position { get; set; }
}
