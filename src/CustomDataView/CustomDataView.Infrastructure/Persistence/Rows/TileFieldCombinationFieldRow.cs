namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileFieldCombinationFieldRow
{
    public Guid TileId { get; set; }

    public string FieldLabel { get; set; }

    public int Position { get; set; }

    public string InputLabel { get; set; }
}
