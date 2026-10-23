namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class ViewRow
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ViewTileRow> Tiles { get; set; } = [];
}
