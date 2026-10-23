namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileActionRow
{
    public Guid TileId { get; set; }

    public string Key { get; set; }

    public int Position { get; set; }

    public string Label { get; set; }

    public string Type { get; set; }

    public Guid TargetTileId { get; set; }

    public string Scope { get; set; }

    public List<TileActionMappingRow> Mappings { get; set; } = [];
}
