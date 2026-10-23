namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileFieldRow
{
    public Guid TileId { get; set; }

    public int Position { get; set; }

    public string SourceKey { get; set; }

    public string Path { get; set; }

    public string Label { get; set; }

    public string NormalizedLabel { get; set; }

    public bool Hidden { get; set; }

    public string Role { get; set; }

    public List<TileFieldTransformationRow> Transformations { get; set; } = [];
}
