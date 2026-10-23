namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileFieldTransformationRow
{
    public Guid TileId { get; set; }

    public int FieldPosition { get; set; }

    public int Position { get; set; }

    public string Key { get; set; }

    public List<TileFieldTransformationParameterRow> Parameters { get; set; } = [];
}
