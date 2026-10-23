namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileFieldCombinationRow
{
    public Guid TileId { get; set; }

    public string FieldLabel { get; set; }

    public string Key { get; set; }

    public string Parameter { get; set; }

    public List<TileFieldCombinationFieldRow> Fields { get; set; } = [];
}
