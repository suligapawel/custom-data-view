namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileRow
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<TileSourceRow> Sources { get; set; } = [];

    public List<TileFieldRow> Fields { get; set; } = [];
}
