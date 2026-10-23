using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TilePaginationPreview(
    int Page,
    int PageSize,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Total,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Pages,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileValues Previous,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileValues Next);
