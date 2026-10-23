namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileFieldTransformationParameterRow
{
    public Guid TileId { get; set; }

    public int FieldPosition { get; set; }

    public int TransformationPosition { get; set; }

    public string Name { get; set; }

    public string Value { get; set; }
}
