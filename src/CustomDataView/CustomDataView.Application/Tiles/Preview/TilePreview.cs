using System.Text.Json.Serialization;

namespace CustomDataView.Application.Tiles.Preview;

public sealed record TilePreview(
    Guid? Id,
    string Name,
    string Display,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TileFieldPreview> Fields,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string ItemKey = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TileItemPreview> Items = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TileFieldError ItemsError = null);
