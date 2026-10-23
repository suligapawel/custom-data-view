namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileFieldTransformationRow
{
    public Guid TileId { get; set; }

    public string FieldLabel { get; set; }

    public int Position { get; set; }

    public string Key { get; set; }

    public string Parameter { get; set; }
}
