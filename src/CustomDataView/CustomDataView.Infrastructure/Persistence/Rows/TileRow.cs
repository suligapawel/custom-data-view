namespace CustomDataView.Infrastructure.Persistence.Rows;

public sealed class TileRow
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Display { get; set; }

    public string ItemsSourceKey { get; set; }

    public string ItemsPath { get; set; }

    public string ItemsKeyLabel { get; set; }

    public string PaginationPageNumberParameter { get; set; }

    public string PaginationPageSizeParameter { get; set; }

    public int? PaginationPageSize { get; set; }

    public string PaginationTotalPath { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<TileSourceRow> Sources { get; set; } = [];

    public List<TileFieldRow> Fields { get; set; } = [];

    public List<TileBindingRow> Bindings { get; set; } = [];

    public List<TileFormFieldRow> FormFields { get; set; } = [];

    public List<TileChildRow> Children { get; set; } = [];
}
