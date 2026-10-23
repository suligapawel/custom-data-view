namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileFormFieldRow
{
    public Guid TileId { get; set; }

    public int Position { get; set; }

    public string Label { get; set; }

    public string NormalizedLabel { get; set; }

    public string Type { get; set; }

    public bool Required { get; set; }

    public string DefaultValue { get; set; }
}
